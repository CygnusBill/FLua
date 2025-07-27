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
.module CompiledLuaScript // GUID = {9DE0E623-C32F-4BFD-8BC7-909619CFC58B}


  .class public auto ansi Program
  	extends [System.Private.CoreLib]System.Object
  {

    // method line 1
    .method public static 
           default int32 Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 1103 (0x44f)
	.maxstack 4
	.locals (
		class [FLua.Runtime]FLua.Runtime.LuaEnvironment	V_0,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_1,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_2,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_3,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_4,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_5,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_6,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_7,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_8,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_9,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_10,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_11,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_12,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_13,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_14,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_15,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_16,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_17,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_18,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_19,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_20,
		class [FLua.Runtime]FLua.Runtime.LuaValue	V_21,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_22,
		class [FLua.Runtime]FLua.Runtime.LuaValue[]	V_23)
	IL_0000:  call class [FLua.Runtime]FLua.Runtime.LuaEnvironment class [FLua.Runtime]FLua.Runtime.LuaEnvironment::CreateStandardEnvironment()
	IL_0005:  stloc 0

	IL_0009:  ldc.i8 0xa
	IL_0012:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0017:  stloc 1

	IL_001b:  ldloc 0

	IL_001f:  ldstr "print"
	IL_0024:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0029:  stloc 2

	IL_002d:  ldc.i4 2
	IL_0032:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0037:  dup 
	IL_0038:  ldc.i4 0
	IL_003d:  ldstr "Before loop: x ="
	IL_0042:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0047:  stelem.ref 
	IL_0048:  dup 
	IL_0049:  ldc.i4 1
	IL_004e:  ldloc 1

	IL_0052:  stelem.ref 
	IL_0053:  stloc 3

	IL_0057:  ldloc 2

	IL_005b:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0060:  dup 
	IL_0061:  brtrue IL_0072

	IL_0066:  pop 
	IL_0067:  ldstr "Attempt to call a non-function value"
	IL_006c:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0071:  throw 
	IL_0072:  nop 
	IL_0073:  ldloc 3

	IL_0077:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_007c:  stloc 4

	IL_0080:  ldloc 4

	IL_0084:  ldlen 
	IL_0085:  conv.i4 
	IL_0086:  ldc.i4.0 
	IL_0087:  ble IL_0097

	IL_008c:  ldloc 4

	IL_0090:  ldc.i4.0 
	IL_0091:  ldelem.ref 
	IL_0092:  br IL_009d

	IL_0097:  nop 
	IL_0098:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_009d:  nop 
	IL_009e:  pop 
	IL_009f:  ldc.i8 0x0
	IL_00a8:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_00ad:  stloc 5

	IL_00b1:  nop 
	IL_00b2:  ldloc 5

	IL_00b6:  ldc.i8 0x2
	IL_00bf:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_00c4:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Less(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_00c9:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_00ce:  brfalse IL_0344

	IL_00d3:  ldloc 5

	IL_00d7:  ldc.i8 0x1
	IL_00e0:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_00e5:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Add(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_00ea:  nop 
	IL_00eb:  stloc 5

	IL_00ef:  ldloc 5

	IL_00f3:  ldc.i8 0x5
	IL_00fc:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_0101:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaOperations::Greater(class [FLua.Runtime]FLua.Runtime.LuaValue, class [FLua.Runtime]FLua.Runtime.LuaValue)
	IL_0106:  call instance bool class [FLua.Runtime]FLua.Runtime.LuaValue::get_IsTruthy()
	IL_010b:  brfalse IL_019e

	IL_0110:  ldloc 0

	IL_0114:  ldstr "print"
	IL_0119:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_011e:  stloc 6

	IL_0122:  ldc.i4 2
	IL_0127:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_012c:  dup 
	IL_012d:  ldc.i4 0
	IL_0132:  ldstr "Breaking due to runaway at counter ="
	IL_0137:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_013c:  stelem.ref 
	IL_013d:  dup 
	IL_013e:  ldc.i4 1
	IL_0143:  ldloc 5

	IL_0147:  stelem.ref 
	IL_0148:  stloc 7

	IL_014c:  ldloc 6

	IL_0150:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0155:  dup 
	IL_0156:  brtrue IL_0167

	IL_015b:  pop 
	IL_015c:  ldstr "Attempt to call a non-function value"
	IL_0161:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0166:  throw 
	IL_0167:  nop 
	IL_0168:  ldloc 7

	IL_016c:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_0171:  stloc 8

	IL_0175:  ldloc 8

	IL_0179:  ldlen 
	IL_017a:  conv.i4 
	IL_017b:  ldc.i4.0 
	IL_017c:  ble IL_018c

	IL_0181:  ldloc 8

	IL_0185:  ldc.i4.0 
	IL_0186:  ldelem.ref 
	IL_0187:  br IL_0192

	IL_018c:  nop 
	IL_018d:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_0192:  nop 
	IL_0193:  pop 
	IL_0194:  br IL_0344

	IL_0199:  br IL_019f

	IL_019e:  nop 
	IL_019f:  nop 
	IL_01a0:  ldloc 0

	IL_01a4:  ldstr "print"
	IL_01a9:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_01ae:  stloc 9

	IL_01b2:  ldc.i4 2
	IL_01b7:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_01bc:  dup 
	IL_01bd:  ldc.i4 0
	IL_01c2:  ldstr "Loop iteration"
	IL_01c7:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_01cc:  stelem.ref 
	IL_01cd:  dup 
	IL_01ce:  ldc.i4 1
	IL_01d3:  ldloc 5

	IL_01d7:  stelem.ref 
	IL_01d8:  stloc 10

	IL_01dc:  ldloc 9

	IL_01e0:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_01e5:  dup 
	IL_01e6:  brtrue IL_01f7

	IL_01eb:  pop 
	IL_01ec:  ldstr "Attempt to call a non-function value"
	IL_01f1:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_01f6:  throw 
	IL_01f7:  nop 
	IL_01f8:  ldloc 10

	IL_01fc:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_0201:  stloc 11

	IL_0205:  ldloc 11

	IL_0209:  ldlen 
	IL_020a:  conv.i4 
	IL_020b:  ldc.i4.0 
	IL_020c:  ble IL_021c

	IL_0211:  ldloc 11

	IL_0215:  ldc.i4.0 
	IL_0216:  ldelem.ref 
	IL_0217:  br IL_0222

	IL_021c:  nop 
	IL_021d:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_0222:  nop 
	IL_0223:  pop 
	IL_0224:  ldloc 0

	IL_0228:  ldstr "print"
	IL_022d:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0232:  stloc 12

	IL_0236:  ldc.i4 2
	IL_023b:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0240:  dup 
	IL_0241:  ldc.i4 0
	IL_0246:  ldstr "  Before assignment: x ="
	IL_024b:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0250:  stelem.ref 
	IL_0251:  dup 
	IL_0252:  ldc.i4 1
	IL_0257:  ldloc 1

	IL_025b:  stelem.ref 
	IL_025c:  stloc 13

	IL_0260:  ldloc 12

	IL_0264:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0269:  dup 
	IL_026a:  brtrue IL_027b

	IL_026f:  pop 
	IL_0270:  ldstr "Attempt to call a non-function value"
	IL_0275:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_027a:  throw 
	IL_027b:  nop 
	IL_027c:  ldloc 13

	IL_0280:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_0285:  stloc 14

	IL_0289:  ldloc 14

	IL_028d:  ldlen 
	IL_028e:  conv.i4 
	IL_028f:  ldc.i4.0 
	IL_0290:  ble IL_02a0

	IL_0295:  ldloc 14

	IL_0299:  ldc.i4.0 
	IL_029a:  ldelem.ref 
	IL_029b:  br IL_02a6

	IL_02a0:  nop 
	IL_02a1:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_02a6:  nop 
	IL_02a7:  pop 
	IL_02a8:  ldc.i8 0x14
	IL_02b1:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaInteger::'.ctor'(int64)
	IL_02b6:  nop 
	IL_02b7:  stloc 1

	IL_02bb:  ldloc 0

	IL_02bf:  ldstr "print"
	IL_02c4:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_02c9:  stloc 15

	IL_02cd:  ldc.i4 2
	IL_02d2:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_02d7:  dup 
	IL_02d8:  ldc.i4 0
	IL_02dd:  ldstr "  After assignment: x ="
	IL_02e2:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_02e7:  stelem.ref 
	IL_02e8:  dup 
	IL_02e9:  ldc.i4 1
	IL_02ee:  ldloc 1

	IL_02f2:  stelem.ref 
	IL_02f3:  stloc 16

	IL_02f7:  ldloc 15

	IL_02fb:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_0300:  dup 
	IL_0301:  brtrue IL_0312

	IL_0306:  pop 
	IL_0307:  ldstr "Attempt to call a non-function value"
	IL_030c:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_0311:  throw 
	IL_0312:  nop 
	IL_0313:  ldloc 16

	IL_0317:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_031c:  stloc 17

	IL_0320:  ldloc 17

	IL_0324:  ldlen 
	IL_0325:  conv.i4 
	IL_0326:  ldc.i4.0 
	IL_0327:  ble IL_0337

	IL_032c:  ldloc 17

	IL_0330:  ldc.i4.0 
	IL_0331:  ldelem.ref 
	IL_0332:  br IL_033d

	IL_0337:  nop 
	IL_0338:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_033d:  nop 
	IL_033e:  pop 
	IL_033f:  br IL_00b1

	IL_0344:  nop 
	IL_0345:  ldloc 0

	IL_0349:  ldstr "print"
	IL_034e:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_0353:  stloc 18

	IL_0357:  ldc.i4 2
	IL_035c:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_0361:  dup 
	IL_0362:  ldc.i4 0
	IL_0367:  ldstr "After loop: x ="
	IL_036c:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_0371:  stelem.ref 
	IL_0372:  dup 
	IL_0373:  ldc.i4 1
	IL_0378:  ldloc 1

	IL_037c:  stelem.ref 
	IL_037d:  stloc 19

	IL_0381:  ldloc 18

	IL_0385:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_038a:  dup 
	IL_038b:  brtrue IL_039c

	IL_0390:  pop 
	IL_0391:  ldstr "Attempt to call a non-function value"
	IL_0396:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_039b:  throw 
	IL_039c:  nop 
	IL_039d:  ldloc 19

	IL_03a1:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_03a6:  stloc 20

	IL_03aa:  ldloc 20

	IL_03ae:  ldlen 
	IL_03af:  conv.i4 
	IL_03b0:  ldc.i4.0 
	IL_03b1:  ble IL_03c1

	IL_03b6:  ldloc 20

	IL_03ba:  ldc.i4.0 
	IL_03bb:  ldelem.ref 
	IL_03bc:  br IL_03c7

	IL_03c1:  nop 
	IL_03c2:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_03c7:  nop 
	IL_03c8:  pop 
	IL_03c9:  ldloc 0

	IL_03cd:  ldstr "print"
	IL_03d2:  call instance class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaEnvironment::GetVariable(string)
	IL_03d7:  stloc 21

	IL_03db:  ldc.i4 2
	IL_03e0:  newarr [FLua.Runtime]FLua.Runtime.LuaValue
	IL_03e5:  dup 
	IL_03e6:  ldc.i4 0
	IL_03eb:  ldstr "After loop: counter ="
	IL_03f0:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaString::'.ctor'(string)
	IL_03f5:  stelem.ref 
	IL_03f6:  dup 
	IL_03f7:  ldc.i4 1
	IL_03fc:  ldloc 5

	IL_0400:  stelem.ref 
	IL_0401:  stloc 22

	IL_0405:  ldloc 21

	IL_0409:  isinst [FLua.Runtime]FLua.Runtime.LuaFunction
	IL_040e:  dup 
	IL_040f:  brtrue IL_0420

	IL_0414:  pop 
	IL_0415:  ldstr "Attempt to call a non-function value"
	IL_041a:  newobj instance void class [FLua.Runtime]FLua.Runtime.LuaRuntimeException::'.ctor'(string)
	IL_041f:  throw 
	IL_0420:  nop 
	IL_0421:  ldloc 22

	IL_0425:  callvirt instance class [FLua.Runtime]FLua.Runtime.LuaValue[] class [FLua.Runtime]FLua.Runtime.LuaFunction::Call(class [FLua.Runtime]FLua.Runtime.LuaValue[])
	IL_042a:  stloc 23

	IL_042e:  ldloc 23

	IL_0432:  ldlen 
	IL_0433:  conv.i4 
	IL_0434:  ldc.i4.0 
	IL_0435:  ble IL_0445

	IL_043a:  ldloc 23

	IL_043e:  ldc.i4.0 
	IL_043f:  ldelem.ref 
	IL_0440:  br IL_044b

	IL_0445:  nop 
	IL_0446:  call class [FLua.Runtime]FLua.Runtime.LuaValue class [FLua.Runtime]FLua.Runtime.LuaValue::get_Nil()
	IL_044b:  nop 
	IL_044c:  pop 
	IL_044d:  ldc.i4.0 
	IL_044e:  ret 
    } // end of method Program::Main

  } // end of class Program

