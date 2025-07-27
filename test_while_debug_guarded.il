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
.module CompiledLuaScript // GUID = {6DFD57D5-C843-4F21-A4C5-DCD3EBD0A053}


  .class public auto ansi Program
  	extends [System.Private.CoreLib]System.Object
  {

    // method line 1
    .method public static 
           default int32 Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 526 (0x20e)
	.maxstack 4
	.locals (
		class [FLua.Runtime]FLua.Runtime.LuaEnvironment	V_0,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_1,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_2,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_3,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_4,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_5,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_6,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_7,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_8,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_9,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_10)
	IL_0000:  call class [FLua.Runtime]FLua.Runtime.LuaEnvironment class [FLua.Runtime]FLua.Runtime.LuaEnvironment::CreateStandardEnvironment()
	IL_0005:  stloc 0

	IL_0009:  ldc.i8 0x0
	IL_0012:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0017:  stloc 1

	IL_001b:  nop 
	IL_001c:  ldloc 1

	IL_0020:  ldc.i8 0x3
	IL_0029:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_002e:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Less(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_0033:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_0038:  brfalse IL_0187

	IL_003d:  ldloc 0

	IL_0041:  ldstr "print"
	IL_0046:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_004b:  stloc 2

	IL_004f:  ldc.i4 2
	IL_0054:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0059:  dup 
	IL_005a:  ldc.i4 0
	IL_005f:  ldstr "i ="
	IL_0064:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0069:  stelem.ref 
	IL_006a:  dup 
	IL_006b:  ldc.i4 1
	IL_0070:  ldloc 1

	IL_0074:  stelem.ref 
	IL_0075:  stloc 3

	IL_0079:  ldloc 2

	IL_007d:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0082:  dup 
	IL_0083:  brtrue IL_0094

	IL_0088:  pop 
	IL_0089:  ldstr "Attempt to call a non-function value"
	IL_008e:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0093:  throw 
	IL_0094:  nop 
	IL_0095:  ldloc 3

	IL_0099:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_009e:  stloc 4

	IL_00a2:  ldloc 4

	IL_00a6:  ldlen 
	IL_00a7:  conv.i4 
	IL_00a8:  ldc.i4.0 
	IL_00a9:  ble IL_00b9

	IL_00ae:  ldloc 4

	IL_00b2:  ldc.i4.0 
	IL_00b3:  ldelem.ref 
	IL_00b4:  br IL_00bf

	IL_00b9:  nop 
	IL_00ba:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_00bf:  nop 
	IL_00c0:  pop 
	IL_00c1:  ldloc 1

	IL_00c5:  ldc.i8 0x5
	IL_00ce:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_00d3:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Greater(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_00d8:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_00dd:  brfalse IL_0165

	IL_00e2:  ldloc 0

	IL_00e6:  ldstr "print"
	IL_00eb:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_00f0:  stloc 5

	IL_00f4:  ldc.i4 1
	IL_00f9:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_00fe:  dup 
	IL_00ff:  ldc.i4 0
	IL_0104:  ldstr "Safety break!"
	IL_0109:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_010e:  stelem.ref 
	IL_010f:  stloc 6

	IL_0113:  ldloc 5

	IL_0117:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_011c:  dup 
	IL_011d:  brtrue IL_012e

	IL_0122:  pop 
	IL_0123:  ldstr "Attempt to call a non-function value"
	IL_0128:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_012d:  throw 
	IL_012e:  nop 
	IL_012f:  ldloc 6

	IL_0133:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_0138:  stloc 7

	IL_013c:  ldloc 7

	IL_0140:  ldlen 
	IL_0141:  conv.i4 
	IL_0142:  ldc.i4.0 
	IL_0143:  ble IL_0153

	IL_0148:  ldloc 7

	IL_014c:  ldc.i4.0 
	IL_014d:  ldelem.ref 
	IL_014e:  br IL_0159

	IL_0153:  nop 
	IL_0154:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_0159:  nop 
	IL_015a:  pop 
	IL_015b:  br IL_0187

	IL_0160:  br IL_0166

	IL_0165:  nop 
	IL_0166:  nop 
	IL_0167:  ldloc 1

	IL_016b:  ldc.i8 0x1
	IL_0174:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0179:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Add(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_017e:  stloc 1

	IL_0182:  br IL_001b

	IL_0187:  nop 
	IL_0188:  ldloc 0

	IL_018c:  ldstr "print"
	IL_0191:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0196:  stloc 8

	IL_019a:  ldc.i4 2
	IL_019f:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_01a4:  dup 
	IL_01a5:  ldc.i4 0
	IL_01aa:  ldstr "Done, i ="
	IL_01af:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_01b4:  stelem.ref 
	IL_01b5:  dup 
	IL_01b6:  ldc.i4 1
	IL_01bb:  ldloc 1

	IL_01bf:  stelem.ref 
	IL_01c0:  stloc 9

	IL_01c4:  ldloc 8

	IL_01c8:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_01cd:  dup 
	IL_01ce:  brtrue IL_01df

	IL_01d3:  pop 
	IL_01d4:  ldstr "Attempt to call a non-function value"
	IL_01d9:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_01de:  throw 
	IL_01df:  nop 
	IL_01e0:  ldloc 9

	IL_01e4:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_01e9:  stloc 10

	IL_01ed:  ldloc 10

	IL_01f1:  ldlen 
	IL_01f2:  conv.i4 
	IL_01f3:  ldc.i4.0 
	IL_01f4:  ble IL_0204

	IL_01f9:  ldloc 10

	IL_01fd:  ldc.i4.0 
	IL_01fe:  ldelem.ref 
	IL_01ff:  br IL_020a

	IL_0204:  nop 
	IL_0205:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_020a:  nop 
	IL_020b:  pop 
	IL_020c:  ldc.i4.0 
	IL_020d:  ret 
    } // end of method Program::Main

  } // end of class Program

