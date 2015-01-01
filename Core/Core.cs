using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelliSync
{
    public class Core
    {
        public static string AppName = "IntelliSync";
    }

    public class FileStatusInformation
    {
        public FileStatusInformation(FILESTATUS status, string information)
        {
            Status = status;
            Information = information;
        }
        public FILESTATUS Status;
        public string Information;
    }
}
