using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace DataExplorerWPF
    {
    [Serializable]
    public class CustomExceptions : Exception
        {
        //File Exception thrown if file is not .mdb
        public string FileException()
            {
            string eMsg = "File is not an Access .mdb Database File!";
            return (eMsg);
            }
        }
    }










