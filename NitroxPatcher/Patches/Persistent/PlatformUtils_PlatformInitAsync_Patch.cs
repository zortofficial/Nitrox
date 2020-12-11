using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using NitroxModel.Helper;

namespace NitroxPatcher.Patches.Persistent
{
    public class PlatformUtils_PlatformInitAsync_Patch : NitroxPatch, IPersistentPatch
    {
        public static readonly Type TARGET_CLASS = typeof(PlatformUtils);
        public static readonly MethodInfo TARGET_METHOD = GetMethod();

        public static readonly OpCode INJECTION_OPCODE = OpCodes.Call;
        public static readonly object INJECTION_OPERAND = typeof(UnityEngine.Application).GetMethod("get_isEditor", BindingFlags.Public | BindingFlags.Static);

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> codeInstructions)
        {            
            Validate.NotNull(INJECTION_OPERAND);

            List<CodeInstruction> instructions = codeInstructions.ToList();

            for (int i = 0; i < instructions.Count; i++)
            {
                CodeInstruction current = instructions[i];

                yield return current;

                if (current.opcode.Equals(INJECTION_OPCODE) && current.operand.Equals(INJECTION_OPERAND))
                {
                    // Change isEditor = true jump to isEditor = false to force us into local configuration
                    CodeInstruction next = instructions[++i];
                    next.opcode = OpCodes.Brfalse;
                    yield return next;
                }
            }
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchTranspiler(harmony, TARGET_METHOD);
        }

        private static Type GetLoadAsyncEnumerableMethod()
        {
            Type[] nestedTypes = TARGET_CLASS.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
            Type targetEnumeratorClass = null;

            foreach (Type type in nestedTypes)
            {
                if (type.FullName?.Contains("PlatformInitAsync") == true)
                {
                    targetEnumeratorClass = type;
                }
            }

            Validate.NotNull(targetEnumeratorClass);
            return targetEnumeratorClass;
        }

        private static MethodInfo GetMethod()
        {
            MethodInfo method = GetLoadAsyncEnumerableMethod().GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
            Validate.NotNull(method);

            return method;
        }
    }
}
