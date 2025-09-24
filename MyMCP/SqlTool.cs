using System;
using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;

[McpServerToolType]
public class SqlTool 
{
    private readonly string? _connectionString;
    public SqlTool()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _connectionString = config.GetConnectionString("Yours");
    }

    [McpServerTool, Description("Execute SQL query and return results as JSON")]
    public string ExecuteQuery(string sql)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    // Try to execute as a query that returns results
                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            var results = new List<Dictionary<string, object>>();
                            var columns = new List<string>();

                            // Get column names
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                columns.Add(reader.GetName(i));
                            }

                            // Get data
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                results.Add(row);
                            }

                            stopwatch.Stop();

                            var response = new
                            {
                                success = true,
                                executionTimeMs = stopwatch.ElapsedMilliseconds,
                                rowCount = results.Count,
                                columns = columns,
                                data = results
                            };

                            return JsonSerializer.Serialize(response, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Not a SELECT query, try ExecuteNonQuery
                        int affected = command.ExecuteNonQuery();
                        stopwatch.Stop();

                        var response = new
                        {
                            success = true,
                            executionTimeMs = stopwatch.ElapsedMilliseconds,
                            message = $"Non-query executed. Rows affected: {affected}"
                        };

                        return JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var response = new
            {
                success = false,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                error = ex.Message,
                stackTrace = ex.StackTrace
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    [McpServerTool, Description("Compare two SQL queries and show differences")]
    public string CompareQueries(string originalQuery, string optimizedQuery)
    {
        var originalResult = ExecuteQueryInternal(originalQuery);
        var optimizedResult = ExecuteQueryInternal(optimizedQuery);

        // Analyze differences
        var analysis = AnalyzeDifferences(originalResult, optimizedResult);

        var comparison = new
        {
            original = originalResult,
            optimized = optimizedResult,
            comparison = new
            {
                timestamp = DateTime.UtcNow,
                analysis = analysis
            }
        };

        return JsonSerializer.Serialize(comparison, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool, Description("Test database connection")]
    public string TestConnection()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                stopwatch.Stop();

                var response = new
                {
                    success = true,
                    executionTimeMs = stopwatch.ElapsedMilliseconds,
                    message = "Connection successful",
                    serverVersion = connection.ServerVersion,
                    database = connection.Database
                };

                return JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var response = new
            {
                success = false,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                error = ex.Message
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    [McpServerTool, Description("List all tables in the database")]
    public string ListTables()
    {
        const string query = @"
            SELECT 
                TABLE_SCHEMA,
                TABLE_NAME,
                TABLE_TYPE
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        return ExecuteQuery(query);
    }

    // Private helper methods
    private dynamic ExecuteQueryInternal(string sql)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<Dictionary<string, object>>();
                        var columns = new List<string>();

                        // Get column names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        // Get data
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }

                        stopwatch.Stop();

                        return new
                        {
                            success = true,
                            executionTimeMs = stopwatch.ElapsedMilliseconds,
                            rowCount = results.Count,
                            columns = columns,
                            data = results
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new
            {
                success = false,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                error = ex.Message
            };
        }
    }

    private string AnalyzeDifferences(dynamic original, dynamic optimized)
    {
        var analysis = new StringBuilder();

        if (original.success && optimized.success)
        {
            analysis.AppendLine($"Original Query: {original.rowCount} rows, {original.executionTimeMs}ms");
            analysis.AppendLine($"Optimized Query: {optimized.rowCount} rows, {optimized.executionTimeMs}ms");
            analysis.AppendLine();

            if (original.rowCount != optimized.rowCount)
            {
                analysis.AppendLine($"??  DIFFERENCE: Row count mismatch!");
                analysis.AppendLine($"   Original: {original.rowCount} rows");
                analysis.AppendLine($"   Optimized: {optimized.rowCount} rows");
                analysis.AppendLine($"   Difference: {Math.Abs(original.rowCount - optimized.rowCount)} rows");
            }
            else
            {
                analysis.AppendLine("? Row counts match");
            }

            var performanceDiff = original.executionTimeMs - optimized.executionTimeMs;
            if (performanceDiff > 0)
            {
                analysis.AppendLine($"? Performance improvement: {performanceDiff}ms faster ({Math.Round((double)performanceDiff / original.executionTimeMs * 100, 1)}%)");
            }
            else if (performanceDiff < 0)
            {
                analysis.AppendLine($"??  Performance regression: {Math.Abs(performanceDiff)}ms slower");
            }
            else
            {
                analysis.AppendLine("? Performance is the same");
            }
        }
        else
        {
            analysis.AppendLine("? One or both queries failed to execute");
            if (!original.success)
                analysis.AppendLine($"Original query error: {original.error}");
            if (!optimized.success)
                analysis.AppendLine($"Optimized query error: {optimized.error}");
        }

        return analysis.ToString();
    }
}