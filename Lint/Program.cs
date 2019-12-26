/*
 * Created by SharpDevelop.
 * User: 
 * Date: 2019/12/25
 * Time: 15:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Lint
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			
			using (var engine = new Engine())
			{
			    // Set and print the value of global 'x'
			    engine["x"] = 25;
			    Console.WriteLine("The value of 'x' is " + engine["x"] + "");
			
			    // Evaluate the following expression: return 25
			    var result = engine.DoString("return x")[0];
			    Console.WriteLine(result);
			}			
			
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}