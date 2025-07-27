.assembly extern FLua.Runtime
{
  .ver 1:0:0:0
}
.assembly extern System.Private.CoreLib
{
  .ver 10:0:0:0
  .publickeytoken = (7C EC 85 D7 BE A7 79 8E ) // |.....y.
}
.assembly 'CompiledLuaScript'
{
  .hash algorithm 0x00000000
  .ver  1:0:0:0
}
.module CompiledLuaScript // GUID = {0C4BEF4A-E786-4688-A17C-F435F4E5E288}


  .class public auto ansi CompiledLuaScript
  	extends [System.Private.CoreLib]System.Object
  {

  .class nested public auto ansi LuaScript
  	extends [System.Private.CoreLib]System.Object
  {

    // method line 1
    .method public static 
           default class [FLua.Runtime]FLua.Runtime.LuaValue[] Execute (class [FLua.Runtime]FLua.Runtime.LuaEnvironment env)  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 361 (0x169)
	.maxstack 4
	.locals (
		class [FLua.Runtime]FLua.Runtime.LuaEnvironment	V_0,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_1,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_2,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_3,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_4,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_5,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_6,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_7)
	IL_0000:  ldarg.0 
	IL_0001:  stloc 0

	IL_0005:  ldc.i8 0x0
	IL_000e:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0013:  stloc 1

	IL_0017:  nop 
	IL_0018:  ldloc 1

	IL_001c:  ldc.i8 0x3
	IL_0025:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_002a:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Less(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_002f:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_0034:  brfalse IL_00dd

	IL_0039:  ldloc 0

	IL_003d:  ldstr "print"
	IL_0042:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0047:  stloc 2

	IL_004b:  ldc.i4 2
	IL_0050:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0055:  dup 
	IL_0056:  ldc.i4 0
	IL_005b:  ldstr "i ="
	IL_0060:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0065:  stelem.ref 
	IL_0066:  dup 
	IL_0067:  ldc.i4 1
	IL_006c:  ldloc 1

	IL_0070:  stelem.ref 
	IL_0071:  stloc 3

	IL_0075:  ldloc 2

	IL_0079:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_007e:  dup 
	IL_007f:  brtrue IL_0090

	IL_0084:  pop 
	IL_0085:  ldstr "Attempt to call a non-function value"
	IL_008a:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_008f:  throw 
	IL_0090:  nop 
	IL_0091:  ldloc 3

	IL_0095:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_009a:  stloc 4

	IL_009e:  ldloc 4

	IL_00a2:  ldlen 
	IL_00a3:  conv.i4 
	IL_00a4:  ldc.i4.0 
	IL_00a5:  ble IL_00b5

	IL_00aa:  ldloc 4

	IL_00ae:  ldc.i4.0 
	IL_00af:  ldelem.ref 
	IL_00b0:  br IL_00bb

	IL_00b5:  nop 
	IL_00b6:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_00bb:  nop 
	IL_00bc:  pop 
	IL_00bd:  ldloc 1

	IL_00c1:  ldc.i8 0x1
	IL_00ca:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_00cf:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Add(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_00d4:  stloc 1

	IL_00d8:  br IL_0017

	IL_00dd:  nop 
	IL_00de:  ldloc 0

	IL_00e2:  ldstr "print"
	IL_00e7:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_00ec:  stloc 5

	IL_00f0:  ldc.i4 2
	IL_00f5:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_00fa:  dup 
	IL_00fb:  ldc.i4 0
	IL_0100:  ldstr "Done, i ="
	IL_0105:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_010a:  stelem.ref 
	IL_010b:  dup 
	IL_010c:  ldc.i4 1
	IL_0111:  ldloc 1

	IL_0115:  stelem.ref 
	IL_0116:  stloc 6

	IL_011a:  ldloc 5

	IL_011e:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0123:  dup 
	IL_0124:  brtrue IL_0135

	IL_0129:  pop 
	IL_012a:  ldstr "Attempt to call a non-function value"
	IL_012f:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0134:  throw 
	IL_0135:  nop 
	IL_0136:  ldloc 6

	IL_013a:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_013f:  stloc 7

	IL_0143:  ldloc 7

	IL_0147:  ldlen 
	IL_0148:  conv.i4 
	IL_0149:  ldc.i4.0 
	IL_014a:  ble IL_015a

	IL_014f:  ldloc 7

	IL_0153:  ldc.i4.0 
	IL_0154:  ldelem.ref 
	IL_0155:  br IL_0160

	IL_015a:  nop 
	IL_015b:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_0160:  nop 
	IL_0161:  pop 
	IL_0162:  ldc.i4.0 
	IL_0163:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0168:  ret 
    } // end of method LuaScript::Execute

  } // end of class LuaScript

  } // end of class CompiledLuaScript

