using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMCP
{
    [McpServerToolType]
    public static class EchoTool
    {
        [McpServerTool, Description("message to client")]
        public static string Echo(string message) => "Hello";
        [McpServerTool, Description("message to client")]
        public static string ReverseEcho(string message) => new string (message.Reverse().ToArray());



    }
}
