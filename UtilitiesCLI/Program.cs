using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace UtilitiesCLI
{
	internal class Program
	{
		private static List<string> forbiddenwords;

		static void Main(string[] args)
		{
			Console.WriteLine("Anonymize PDF");

			var sourceFile = args[0];
			var targetFile = args[1];
			forbiddenwords = args.Skip(2).ToList();

			File.Copy(sourceFile, targetFile, true);
			using (var document = PdfReader.Open(targetFile, PdfDocumentOpenMode.Modify))
			{
				foreach (var page in document.Pages)
				{
					var contents = ContentReader.ReadContent(page);
					foreach (var item in contents)
					{
						ReplaceText(item);
					}
				}

				// Save PDF with new name.
				document.Save(targetFile);
			}
		}

		private static void ReplaceText(CObject obj)
		{
			if (obj is CArray)
				ReplaceText((CArray)obj);
			else if (obj is CComment)
				ReplaceText((CComment)obj);
			else if (obj is CInteger)
				ReplaceText((CInteger)obj);
			else if (obj is CName)
				ReplaceText((CName)obj);
			else if (obj is CNumber)
				ReplaceText((CNumber)obj);
			else if (obj is COperator)
				ReplaceText((COperator)obj);
			else if (obj is CReal)
				ReplaceText((CReal)obj);
			else if (obj is CSequence)
				ReplaceText((CSequence)obj);
			else if (obj is CString)
				ReplaceText((CString)obj);
			else
				throw new NotImplementedException(obj.GetType().AssemblyQualifiedName);
		}
		private static void ReplaceText(CArray obj)
		{
			foreach (var element in obj)
			{
				ReplaceText(element);
			}
		}
		private static void ReplaceText(CComment obj) { /* nothing */ }
		private static void ReplaceText(CInteger obj) { /* nothing */ }
		private static void ReplaceText(CName obj) { /* nothing */ }
		private static void ReplaceText(CNumber obj) { /* nothing */ }
		private static void ReplaceText(COperator obj)
		{
			if (obj.OpCode.OpCodeName == OpCodeName.Tj || obj.OpCode.OpCodeName == OpCodeName.TJ)
			{
				foreach (var element in obj.Operands)
				{
					ReplaceText(element);
				}
			}
		}
		private static void ReplaceText(CReal obj) { /* nothing */ }
		private static void ReplaceText(CSequence obj)
		{
			foreach (var element in obj)
			{
				ReplaceText(element);
			}
		}
		private static void ReplaceText(CString obj)
		{
			obj.Value = ReplaceText(forbiddenwords, obj.Value);
		}

		private static string ReplaceText(List<string> forbiddenwords, string text)
		{
			foreach (var word in forbiddenwords)
			{
				text = text.Replace(word, new string('*', word.Length), StringComparison.InvariantCultureIgnoreCase);
			}

			return text;
		}

	}
}
