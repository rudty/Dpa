using System.Reflection;
using System.Reflection.Emit;

namespace Dpa.Repository.Implements.Runtime
{
    internal static partial class RuntimeTypeGenerator
    {
        private static readonly AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("repo_assembly"), 
            AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("repoModule");

        private static int generateCount = 0;
    }
}
