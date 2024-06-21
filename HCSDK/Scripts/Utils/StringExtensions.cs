using UnityEngine;
using System.Collections;

namespace HCExtension
{

	public static class StringExtensions 
	{
		/// <summary>
		/// Splits string without empty entries. Method will invoke Split with option RemoveEmptyEntries
		/// and will create array with your separator.
		/// </summary>
		/// <returns>
		/// Array with strings after split with option RemoveEmptyEntries.
		/// </returns>
		/// <param name='text'>
		/// String to split.
		/// </param>
		/// <param name='separator'>
		/// Separator.
		/// </param>
		public static string[] SplitWithoutEmpty(this string text, char separator)
		{
			return text.Split(new char[]{separator},System.StringSplitOptions.RemoveEmptyEntries);
		}
		
		public static string[] Split(this string text, string separator)
		{
			return text.Split(new string[]{separator},System.StringSplitOptions.None);
		}

		public static string[] Split(this string text, char separator, int count)
		{
			return text.Split(new char[] { separator }, count);
		}
		
	}
		
}
