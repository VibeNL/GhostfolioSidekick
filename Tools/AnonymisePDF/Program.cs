﻿using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.PdfCleanup;
using iText.PdfCleanup.Autosweep;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Tools.AnonymisePDF
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var sourceFile = args[0];
			var targetFile = args[1];
			var forbiddenWords = args.Skip(2);

			using (var pdf = new PdfDocument(new PdfReader(sourceFile), new PdfWriter(targetFile)))
			{
				foreach (var word in forbiddenWords)
				{
					var cleanupStrategy = new RegexBasedCleanupStrategy(new Regex(word, RegexOptions.IgnoreCase)).SetRedactionColor(ColorConstants.RED);
					PdfCleaner.AutoSweepCleanUp(pdf, cleanupStrategy);
				}

				pdf.Close();
			}
		}
	}
}
