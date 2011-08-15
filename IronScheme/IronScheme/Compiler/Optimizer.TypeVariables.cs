﻿#region License
/* Copyright (c) 2007,2008,2009,2010,2011 Llewellyn Pritchard 
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See docs/license.txt. */
#endregion

using Microsoft.Scripting.Ast;
using System;
using System.Collections.Generic;
using Microsoft.Scripting;
using System.Diagnostics;

namespace IronScheme.Compiler
{
  static partial class Optimizer
  {
    class SimpleTypeVariables : OptimizerBase
    {
      public override void Optimize()
      {
        Pass0 p0 = new Pass0();
        p0.WalkNode(Root);
      }

      class Pass0 : DeepWalker
      {
        Dictionary<Variable, int> writecounts = new Dictionary<Variable, int>();

        protected override bool Walk(WriteStatement node)
        {
          writecounts[node.Variable] = writecounts.ContainsKey(node.Variable) ? writecounts[node.Variable] + 1 : 1;
          return base.Walk(node);
        }

        protected override void PostWalk(WriteStatement node)
        {
          base.PostWalk(node);

          var v = node.Variable;
          var val = node.Value;

          if (v.Type == typeof(object))
          {
            if (val.Type != typeof(object))
            {
              v.Type = val.Type;
            }
          }
          else if (v.Type != val.Type)
          {
            v.Type = typeof(object);
          }
          else
          {
            writecounts[node.Variable]--;
          }

          if (writecounts[node.Variable] > 1)
          {
            v.Type = typeof(object);
          }
        }
      }
    }

    class TypeVariables : OptimizerBase
    {
      public override void Optimize()
      {
        int fc = 0;
        do
        {
          var vartypes = new Dictionary<Variable, Dictionary<Type, List<Expression>>>();
          var p0 = new Pass0 { vartypes = vartypes };
          p0.WalkNode(Root);

          var p1 = new Pass1 { vartypes = vartypes };
          p1.WalkNode(Root);

          fc = p1.Count;
        }
        while (fc > 0);
      }

      static Expression Unwrap(Expression ex)
      {
        while (ex is UnaryExpression && ex.NodeType == AstNodeType.Convert)
        {
          ex = ((UnaryExpression)ex).Operand;
        }

        return ex;
      }

      class Pass0 : DeepWalker
      {
        internal Dictionary<Variable, Dictionary<Type, List<Expression>>> vartypes;

        protected override bool Walk(WriteStatement node)
        {
          var val = node.Value;
          var var = node.Variable;

          ProcessAssignment(val, var);    

          return base.Walk(node);
        }

        protected override bool Walk(BoundAssignment node)
        {
          var val = node.Value;
          var var = node.Variable;

          ProcessAssignment(val, var);  

          return base.Walk(node);
        }

        void ProcessAssignment(Expression val, Variable var)
        {
          Dictionary<Type, List<Expression>> typecounts;

          if (!vartypes.TryGetValue(var, out typecounts))
          {
            vartypes[var] = typecounts = new Dictionary<Type, List<Expression>>();
          }

          val = Unwrap(val);

          List<Expression> exps;

          if (!typecounts.TryGetValue(val.Type, out exps))
          {
            typecounts[val.Type] = exps = new List<Expression>();
          }

          exps.Add(val);
        }
      }

      class Pass1 : DeepWalker
      {
        internal Dictionary<Variable, Dictionary<Type, List<Expression>>> vartypes;

        internal int Count = 0;
        internal Dictionary<CodeBlock, bool> rebinds = new Dictionary<CodeBlock, bool>(); 

        protected override void PostWalk(WriteStatement node)
        {
          base.PostWalk(node);

          var val = node.Value;
          var var = node.Variable;

          val = ProcessAssignment(val, var);
          node.Value = val;
        }

        protected override void PostWalk(BoundAssignment node)
        {
          base.PostWalk(node);

          var val = node.Value;
          var var = node.Variable;

          val = ProcessAssignment(val, var);
          node.Value = val;
        }

        Expression ProcessAssignment(Expression val, Variable var)
        {
          var typecounts = vartypes[var];

          if (var.Type == typeof(object) && var.Kind != Variable.VariableKind.Parameter)
          {
            if (typecounts.Count == 1)
            {
              foreach (var kv in typecounts)
              {
                if (kv.Key != typeof(object) && kv.Key != typeof(bool) && kv.Key != typeof(SymbolId))
                {
                  var.Type = kv.Key;
                  Count++;
                  rebinds[var.Block] = true;
                  return Unwrap(val);
                }
                break;
              }
            }
            else
            {
              // what here?
              //var = node.Variable;
            }
          }

          return val;
        }
      }
    }
  }
}
