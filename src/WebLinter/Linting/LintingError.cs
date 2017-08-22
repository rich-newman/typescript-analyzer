using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLinter
{
    public class LintingError
    {
        public LintingError(string fileName, int lineNumber, int columnNumber, bool isError, string errorCode)
        {
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
            IsError = isError;
            ErrorCode = errorCode;
        }

        public Linter Provider { get; set; }
        public string FileName { get; }
        public string Message { get; set; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public bool IsError { get; } = true;
        public string ErrorCode { get; }
        public string HelpLink { get; set; }

        public override string ToString()
        {
            return Message;
        }

        protected bool Equals(LintingError other)
        {
            return string.Equals(FileName, other.FileName) && LineNumber == other.LineNumber && ColumnNumber == other.ColumnNumber && 
                IsError == other.IsError && string.Equals(ErrorCode, other.ErrorCode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LintingError)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FileName != null ? FileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LineNumber;
                hashCode = (hashCode * 397) ^ ColumnNumber;
                hashCode = (hashCode * 397) ^ IsError.GetHashCode();
                hashCode = (hashCode * 397) ^ (ErrorCode != null ? ErrorCode.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
