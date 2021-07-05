using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Town_Crier.Database
{
	public class EnumToStringConverter<T> : IPropertyConverter
		where T : struct
	{
		public EnumToStringConverter()
		{
		}

		public object FromEntry(DynamoDBEntry entry)
		{
			Primitive primitive = entry as Primitive;

			if (primitive == null)
			{
				throw new ArgumentException("Enum not stored correctly in database: " + typeof(T));
			}

			string stringValue = primitive.AsString();

			bool wasValid = Enum.TryParse<T>(stringValue, out T result);

			if (wasValid)
			{
				return result;
			}
			else
			{
				Console.WriteLine("Failed parsing DB entry as Enum: {0} with value: {1}", typeof(T).Name, stringValue);

				return default(T);
			}
		}

		public DynamoDBEntry ToEntry(object valueToConvert)
		{
			try
			{
				T genericValue = (T)valueToConvert;
			}
			catch
			{
				throw new ArgumentException("Enum value is not of type: " + typeof(T));
			}

			return new Primitive()
			{
				Value = valueToConvert.ToString()
			};
		}
	}
}
