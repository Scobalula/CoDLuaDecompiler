using System.Collections.Generic;
using CoDLuaDecompiler.Decompiler.IR.Expression;
using CoDLuaDecompiler.Decompiler.IR.Identifiers;
using CoDLuaDecompiler.Decompiler.LuaFile.Structures.LuaFunction.Structures;

namespace CoDLuaDecompiler.Decompiler.IR.Instruction
{
    public class Assignment : IInstruction
    {
        /// <summary>
        /// Functions can have multiple returns
        /// </summary>
        public List<IdentifierReference> Left { get; set; }
        /// <summary>
        /// The expression after the = or in
        /// </summary>
        public IExpression Right { get; set; }

        public List<Local> LocalAssignments { get; set; }
        /// <summary>
        /// Is the first assignment of a local variable, and thus starts with "local"
        /// </summary>
        public bool IsLocalDeclaration { get; set; } = false;
        /// <summary>
        /// If true, the assignment uses "in" instead of "="
        /// </summary>
        public bool IsGenericForAssignment { get; set; } = false;
        /// <summary>
        /// This assignment represents an assignment to an indeterminate number of varargs
        /// </summary>
        public bool IsIndeterminateVararg { get; set; } = false;
        public uint VarargAssignmentReg { get; set; } = 0;
        /// <summary>
        /// When this is set to true, the value defined by this is always expression/constant propagated, even if it's used more than once
        /// </summary>
        public bool PropagateAlways { get; set; } = false;
        /// <summary>
        /// If true, this is a list assignment which affects how expression propagation is done
        /// </summary>
        public bool IsListAssignment { get; set; } = false;
        /// <summary>
        /// If true, this assignment was generated by a self op, which gives some information on how expression propagation is done and the original syntax
        /// </summary>
        public bool IsSelfAssignment { get; set; } = false;
        
        public Assignment(Identifier identifier, IExpression expression)
        {
            Left = new List<IdentifierReference>() {new IdentifierReference(identifier)};
            Right = expression;
        }
        
        public Assignment(IdentifierReference identifier, IExpression expression)
        {
            Left = new List<IdentifierReference>() { identifier };
            Right = expression;
        }
        
        public Assignment(List<IdentifierReference> identifiers, IExpression expression)
        {
            Left = identifiers;
            Right = expression;
        }
        
        public override void Parenthesize()
        {
            Left.ForEach(x => x.Parenthesize());
            if (Right != null)
            {
                Right.Parenthesize();
            }
        }
        
        public override HashSet<Identifier> GetDefines(bool regOnly)
        {
            var defines = new HashSet<Identifier>();
            foreach (var id in Left)
            {
                // If the reference is not an indirect one (i.e. not an array access), then it is a definition
                if (!id.HasIndex && (!regOnly || id.Identifier.IdentifierType == IdentifierType.Register))
                {
                    defines.Add(id.Identifier);
                }
            }
            return defines;
        }
        
        public override HashSet<Identifier> GetUses(bool regOnly)
        {
            var uses = new HashSet<Identifier>();
            foreach (var id in Left)
            {
                // If the reference is an indirect one (i.e. an array access), then it is a use
                if (id.HasIndex /*&& (!regOnly || id.Identifier.IdentifierType == IdentifierType.Register)*/)
                {
                    uses.UnionWith(id.GetUses(regOnly));
                    foreach (var idx in id.TableIndices)
                    {
                        uses.UnionWith(idx.GetUses(regOnly));
                    }
                }
            }
            uses.UnionWith(Right.GetUses(regOnly));
            return uses;
        }
        
        public override void RenameDefines(Identifier orig, Identifier newId)
        {
            foreach (var id in Left)
            {
                // If the reference is not an indirect one (i.e. not an array access), then it is a definition
                if (!id.HasIndex && id.Identifier == orig)
                {
                    id.Identifier = newId;
                    id.Identifier.DefiningInstruction = this;
                }
            }
        }
        
        public override void RenameUses(Identifier orig, Identifier newId)
        {
            foreach (var id in Left)
            {
                // If the reference is an indirect one (i.e. an array access), then it is a use
                if (id.HasIndex)
                {
                    id.RenameUses(orig, newId);
                }
            }
            Right.RenameUses(orig, newId);
        }
        
        public override bool ReplaceUses(Identifier orig, IExpression sub)
        {
            bool replaced = false;
            foreach (var l in Left)
            {
                replaced = replaced || l.ReplaceUses(orig, sub);
            }
            if (IExpression.ShouldReplace(orig, Right))
            {
                replaced = true;
                Right = sub;
            }
            else
            {
                replaced = replaced || Right.ReplaceUses(orig, sub);
            }
            return replaced;
        }
        
        public override List<IExpression> GetExpressions()
        {
            var ret = new List<IExpression>();
            foreach (var left in Left)
            {
                ret.AddRange(left.GetExpressions());
            }

            if (Right != null)
                ret.AddRange(Right.GetExpressions());
            return ret;
        }

        public override string ToString()
        {
            var str = "";
            if (IsLocalDeclaration)
            {
                str = "local ";
            }
            if (Left.Count == 1 && !Left[0].HasIndex && !Left[0].DotNotation && Left[0].Identifier.IdentifierType == IdentifierType.Global && Right is Closure c)
            {
                return c.Function.PrettyPrint(Left[0].Identifier.Name);
            }
            if (Left.Count > 0)
            {
                var assignmentOp = IsGenericForAssignment ? " in " : " = ";
                if (Left.Count == 1 && Left[0].HasIndex && Right is Closure)
                {
                    Left[0].DotNotation = true;
                    str = Left[0] + assignmentOp + Right;
                }
                else
                {
                    for (int i = 0; i < Left.Count; i++)
                    {
                        str += Left[i].ToString();
                        if (i != Left.Count - 1)
                        {
                            str += ", ";
                        }
                    }
                    if (Right != null)
                    {
                        str += assignmentOp + Right;
                    }
                }
            }
            else
            {
                str = Right.ToString();
            }

#if DEBUG
            if (LocalAssignments != null)
            {
                var locals = "";
                foreach (var assignment in LocalAssignments)
                {
                    locals += assignment.Name + ", ";
                }
                if (LocalAssignments.Count > 0)
                    str += $" --[[ {locals}]]";
            }
            str += $" --[[ @ {LineLocation}]]";
#endif

            return str;
        }
    }
}