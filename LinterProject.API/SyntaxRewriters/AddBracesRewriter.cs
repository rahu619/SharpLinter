using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OnlineLinter.SyntaxRewriters
{
  public class AddBracesRewriter : CSharpSyntaxRewriter
  {
    public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
    {
      var newIfStatement = node.WithStatement(AddBracesToStatement(node.Statement));

      if (newIfStatement.Else != null)
      {
        var newElseClause = newIfStatement.Else.WithStatement(AddBracesToStatement(newIfStatement.Else.Statement));
        newIfStatement = newIfStatement.WithElse(newElseClause);
      }

      return base.VisitIfStatement(newIfStatement);
    }

    public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
    {
      return node.WithStatement(AddBracesToStatement(node.Statement));
    }

    public override SyntaxNode VisitForStatement(ForStatementSyntax node)
    {
      return node.WithStatement(AddBracesToStatement(node.Statement));
    }

    public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
    {
      return node.WithStatement(AddBracesToStatement(node.Statement));
    }

    public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
    {
      return node.WithStatement(AddBracesToStatement(node.Statement));
    }

    private StatementSyntax AddBracesToStatement(StatementSyntax statement)
    {
      if (statement is BlockSyntax)
      {
        return statement;
      }

      return SyntaxFactory.Block(statement);
    }
  }
}
