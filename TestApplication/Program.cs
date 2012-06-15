using System;
using CalculatorCompiler;
using System.CodeDom.Compiler;

namespace TestApplication
{
    class Program
    {
        static void Main( string[ ] args )
        {
            float x = 42f;
            Function f;
            CalculatorFactory fact = new CalculatorFactory( );

            while ( true )
            {
                Console.Write( "> " );
                string s = Console.ReadLine( );

                try
                {
                    f = fact.Get( s );

                    Console.WriteLine( f( x ) );
                }
                catch ( CompileException e )
                {
                    Console.WriteLine( "Errors:" );

                    foreach ( CompilerError err in e.Errors )
                        Console.WriteLine( err.ErrorText );
                }
            }
        }
    }
}
