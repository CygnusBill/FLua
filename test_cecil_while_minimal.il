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
.module CompiledLuaScript // GUID = {BCADF8CB-3249-48AD-992A-D2F1DFB77F99}


  .class public auto ansi Program
  	extends [System.Private.CoreLib]System.Object
  {

    // method line 1
    .method public static 
           default int32 Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 562 (0x232)
	.maxstack 4
	.locals (
		class [FLua.Runtime]FLua.Runtime.LuaEnvironment	V_0,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_1,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_2,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_3,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_4,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_5,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_6,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_7,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_8,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_9,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_10,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_11)
	IL_0000:  call class [FLua.Runtime]FLua.Runtime.LuaEnvironment class [FLua.Runtime]FLua.Runtime.LuaEnvironment::CreateStandardEnvironment()
	IL_0005:  stloc 0

	IL_0009:  ldc.i8 0x0
	IL_0012:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0017:  stloc 1

	IL_001b:  ldc.i8 0x0
	IL_0024:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0029:  stloc 2

	IL_002d:  nop 
	IL_002e:  ldloc 1

	IL_0032:  ldc.i8 0x1
	IL_003b:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0040:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Less(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_0045:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_004a:  brfalse IL_01ab

	IL_004f:  ldloc 2

	IL_0053:  ldc.i8 0x1
	IL_005c:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0061:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Add(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_0066:  stloc 2

	IL_006a:  ldloc 0

	IL_006e:  ldstr "print"
	IL_0073:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0078:  stloc 3

	IL_007c:  ldc.i4 2
	IL_0081:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0086:  dup 
	IL_0087:  ldc.i4 0
	IL_008c:  ldstr "in loop, count ="
	IL_0091:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0096:  stelem.ref 
	IL_0097:  dup 
	IL_0098:  ldc.i4 1
	IL_009d:  ldloc 2

	IL_00a1:  stelem.ref 
	IL_00a2:  stloc 4

	IL_00a6:  ldloc 3

	IL_00aa:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_00af:  dup 
	IL_00b0:  brtrue IL_00c1

	IL_00b5:  pop 
	IL_00b6:  ldstr "Attempt to call a non-function value"
	IL_00bb:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_00c0:  throw 
	IL_00c1:  nop 
	IL_00c2:  ldloc 4

	IL_00c6:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_00cb:  stloc 5

	IL_00cf:  ldloc 5

	IL_00d3:  ldlen 
	IL_00d4:  conv.i4 
	IL_00d5:  ldc.i4.0 
	IL_00d6:  ble IL_00e6

	IL_00db:  ldloc 5

	IL_00df:  ldc.i4.0 
	IL_00e0:  ldelem.ref 
	IL_00e1:  br IL_00ec

	IL_00e6:  nop 
	IL_00e7:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_00ec:  nop 
	IL_00ed:  pop 
	IL_00ee:  ldloc 2

	IL_00f2:  ldc.i8 0x3
	IL_00fb:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0100:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Greater(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_0105:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_010a:  brfalse IL_0192

	IL_010f:  ldloc 0

	IL_0113:  ldstr "print"
	IL_0118:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_011d:  stloc 6

	IL_0121:  ldc.i4 1
	IL_0126:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_012b:  dup 
	IL_012c:  ldc.i4 0
	IL_0131:  ldstr "Breaking due to runaway"
	IL_0136:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_013b:  stelem.ref 
	IL_013c:  stloc 7

	IL_0140:  ldloc 6

	IL_0144:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0149:  dup 
	IL_014a:  brtrue IL_015b

	IL_014f:  pop 
	IL_0150:  ldstr "Attempt to call a non-function value"
	IL_0155:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_015a:  throw 
	IL_015b:  nop 
	IL_015c:  ldloc 7

	IL_0160:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_0165:  stloc 8

	IL_0169:  ldloc 8

	IL_016d:  ldlen 
	IL_016e:  conv.i4 
	IL_016f:  ldc.i4.0 
	IL_0170:  ble IL_0180

	IL_0175:  ldloc 8

	IL_0179:  ldc.i4.0 
	IL_017a:  ldelem.ref 
	IL_017b:  br IL_0186

	IL_0180:  nop 
	IL_0181:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_0186:  nop 
	IL_0187:  pop 
	IL_0188:  br IL_01ab

	IL_018d:  br IL_0193

	IL_0192:  nop 
	IL_0193:  nop 
	IL_0194:  ldc.i8 0x1
	IL_019d:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_01a2:  stloc 1

	IL_01a6:  br IL_002d

	IL_01ab:  nop 
	IL_01ac:  ldloc 0

	IL_01b0:  ldstr "print"
	IL_01b5:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_01ba:  stloc 9

	IL_01be:  ldc.i4 2
	IL_01c3:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_01c8:  dup 
	IL_01c9:  ldc.i4 0
	IL_01ce:  ldstr "done, i ="
	IL_01d3:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_01d8:  stelem.ref 
	IL_01d9:  dup 
	IL_01da:  ldc.i4 1
	IL_01df:  ldloc 1

	IL_01e3:  stelem.ref 
	IL_01e4:  stloc 10

	IL_01e8:  ldloc 9

	IL_01ec:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_01f1:  dup 
	IL_01f2:  brtrue IL_0203

	IL_01f7:  pop 
	IL_01f8:  ldstr "Attempt to call a non-function value"
	IL_01fd:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0202:  throw 
	IL_0203:  nop 
	IL_0204:  ldloc 10

	IL_0208:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_020d:  stloc 11

	IL_0211:  ldloc 11

	IL_0215:  ldlen 
	IL_0216:  conv.i4 
	IL_0217:  ldc.i4.0 
	IL_0218:  ble IL_0228

	IL_021d:  ldloc 11

	IL_0221:  ldc.i4.0 
	IL_0222:  ldelem.ref 
	IL_0223:  br IL_022e

	IL_0228:  nop 
	IL_0229:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_022e:  nop 
	IL_022f:  pop 
	IL_0230:  ldc.i4.0 
	IL_0231:  ret 
    } // end of method Program::Main

  } // end of class Program

