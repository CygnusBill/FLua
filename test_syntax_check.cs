using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

class Test {
    void TestMethod() {
        // Test the syntax that's causing issues
        var paramDecl = LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
                .AddVariables(
                    VariableDeclarator(Identifier("test"))
                        .WithInitializer(EqualsValueClause(
                            ConditionalExpression(
                                BinaryExpression(
                                    SyntaxKind.GreaterThanExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("args"),
                                        IdentifierName("Length")),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                                ElementAccessExpression(IdentifierName("args"))
                                    .AddArgumentListArguments(
                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("LuaNil"),
                                    IdentifierName("Instance")))))));
    }
}