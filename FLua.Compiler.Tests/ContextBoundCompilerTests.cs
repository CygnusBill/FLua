using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;

namespace FLua.Compiler.Tests;

[TestClass]
public class ContextBoundCompilerTests
{
    public record SimpleContext(int Foo, int Bar);
    
    public class Calculator
    {
        public int BaseValue { get; set; } = 10;
        public int FinalScore { get; set; } = 100;
        
        public int CalculateValue(int input)
        {
            return BaseValue * input;
        }
        
        public int AddNumbers(int a, int b)
        {
            return a + b;
        }
    }
    
    public record ComplexContext(Calculator A, int Foo, int Threshold);
    
    [TestMethod]
    public void TestSimpleExpression()
    {
        var snippet = "foo * 2 + bar";
        var context = new SimpleContext(5, 3);
        
        var func = ContextBoundCompiler.Create<SimpleContext, int>(snippet);
        var result = func(new SimpleContext(5, 3));
        
        Assert.AreEqual(13, result); // 5 * 2 + 3 = 13
    }
    
    [TestMethod]
    public void TestPropertyAccess()
    {
        var snippet = "a.final_score * 2";
        var context = new ComplexContext(new Calculator(), 0, 0);
        
        var func = ContextBoundCompiler.Create<ComplexContext, int>(snippet);
        var result = func(context);
        
        Assert.AreEqual(200, result); // 100 * 2 = 200
    }
    
    [TestMethod]
    public void TestMethodCall()
    {
        var snippet = "a.calculate_value(foo)";
        var calc = new Calculator { BaseValue = 10 };
        var context = new ComplexContext(calc, 5, 0);
        
        var func = ContextBoundCompiler.Create<ComplexContext, int>(snippet);
        var result = func(context);
        
        Assert.AreEqual(50, result); // 10 * 5 = 50
    }
    
    [TestMethod]
    public void TestConditionalLogic()
    {
        var snippet = @"
            local result = a.calculate_value(foo)
            if result > threshold then
                return a.final_score * 2
            else
                return foo
            end
        ";
        
        var calc = new Calculator { BaseValue = 10, FinalScore = 100 };
        
        // Test when result > threshold
        var context1 = new ComplexContext(calc, 5, 40);
        var func = ContextBoundCompiler.Create<ComplexContext, int>(snippet);
        var result1 = func(context1);
        Assert.AreEqual(200, result1); // result=50 > 40, so return 100*2
        
        // Test when result <= threshold  
        var context2 = new ComplexContext(calc, 3, 40);
        var result2 = func(context2);
        Assert.AreEqual(3, result2); // result=30 <= 40, so return foo=3
    }
    
    [TestMethod]
    public void TestNameTranslation()
    {
        // Test that snake_case, camelCase, and PascalCase all work
        var snippets = new[] {
            "a.final_score",      // snake_case
            "a.finalScore",       // camelCase  
            "a.FinalScore"        // PascalCase
        };
        
        var context = new ComplexContext(new Calculator(), 0, 0);
        
        foreach (var snippet in snippets)
        {
            var func = ContextBoundCompiler.Create<ComplexContext, int>(snippet);
            var result = func(context);
            Assert.AreEqual(100, result);
        }
    }
}