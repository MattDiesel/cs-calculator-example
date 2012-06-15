using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CalculatorCompiler
{
    public delegate float Function( float x );

    public class CompileException : Exception
    {
        private CompilerErrorCollection errors;

        public CompileException(CompilerResults res)
        {
            this.errors = res.Errors;
        }

        public ICollection Errors
        {
            get
            {
                return this.errors;
            }
        }

        public override string Message
        {
            get
            {
                return String.Format( "{0} errors occurred in compilation.", this.errors.Count );
            }
        }
    }

    public class CalculatorFactory
    {
        private string outstr;
        private HashSet<string> functions;

        public CalculatorFactory()
        {
            this.loadFunctions( );
        }

        private void loadFunctions()
        {
            this.functions = new HashSet<string>( );

            Assembly sys = Assembly.GetAssembly( typeof( System.Math ) );

            foreach ( MethodInfo m in sys
                .GetType( "System.Math" )
                .GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly ) )
            {
                if ( !this.functions.Contains( m.Name ) )
                    this.functions.Add( m.Name );
            }

            this.functions.Add( "PI" );
        }

        public Function Get(string formula)
        {
            Calculator c = new Calculator( );
            this.Create( formula, ref c );

            return c.Execute;
        }

        private void Create(string formula, ref Calculator ret)
        {
            formula = this.PreProcess( formula );

            CodeSnippetExpression snippet = new CodeSnippetExpression( formula );

            CodeMemberMethod meth = new CodeMemberMethod( );
            meth.Name = "Execute";
            meth.Attributes = MemberAttributes.Static | MemberAttributes.Public;
            meth.ReturnType = new CodeTypeReference( typeof( float ) );
            meth.Parameters.Add( new CodeParameterDeclarationExpression( new CodeTypeReference( typeof( float ) ), "x" ) );
            meth.Statements.Add( new CodeMethodReturnStatement( new CodeCastExpression(
                new CodeTypeReference(typeof(float)), snippet ) ) );
            
            CodeTypeDeclaration cl = new CodeTypeDeclaration( );
            cl.IsClass = true;
            cl.Name = "Evaluator";
            cl.Attributes = MemberAttributes.Public;
            cl.Members.Add( meth );

            CodeNamespace namesp = new CodeNamespace( "ExpressionEvaluator" );
            namesp.Imports.Add( new CodeNamespaceImport( "System" ) );
            namesp.Types.Add( cl );

            CodeCompileUnit unit = new CodeCompileUnit( );
            unit.Namespaces.Add( namesp );

            CompilerParameters cparams = new CompilerParameters( );
            cparams.CompilerOptions = "/target:library /optimize";
            cparams.GenerateExecutable = false;
            cparams.GenerateInMemory = true;
            cparams.IncludeDebugInformation = false;
            cparams.ReferencedAssemblies.Add( "mscorlib.dll" );
            cparams.ReferencedAssemblies.Add( "System.dll" );
            cparams.TreatWarningsAsErrors = true;

            using ( CSharpCodeProvider prov = new CSharpCodeProvider( ) )
            {
                StringWriter w = new StringWriter();
                prov.GenerateCodeFromCompileUnit( unit, w, new CodeGeneratorOptions( ) );
                this.PreProcess( w.ToString( ) );

                CompilerResults results = prov.CompileAssemblyFromDom( cparams, unit );

                if ( results.Errors.Count > 0 )
                    throw new CompileException( results );

                ret.assembly = results.CompiledAssembly;

                Type t = ret.assembly.GetType( "ExpressionEvaluator.Evaluator" );

                foreach ( MethodInfo m in t.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly ) )
                {
                    if ( m.Name == "Execute" )
                    {
                        ret.method = m;
                        break;
                    }
                }
            }

            return;
        }

        private string PreProcess(string formula)
        {
            formula = formula.ToLower();

            foreach ( string m in this.functions )
                formula = formula.Replace( m.ToLower( ), "Math." + m );

            return formula;
        }
    }

    /// <summary>
    /// Compiles a statement which can then be invoked.
    /// </summary>
    public class Calculator
    {
        internal Assembly assembly;
        internal MethodInfo method;

        public float Execute(float x)
        {
            return (float)this.method.Invoke( null, new object[ ] { x } );
        }
    }
}
