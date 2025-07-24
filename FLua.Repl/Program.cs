using FLua.Interpreter;

namespace FLua.Repl
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var repl = new LuaRepl();
            repl.Run();
        }
    }
}
