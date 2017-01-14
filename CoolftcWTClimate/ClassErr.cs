using System;
using System.Collections.Generic;
using System.Text;

namespace WSHelpers
{
    public class ClassExp : Exception
    {
        // To use this class: throw new ClassExp(ClassExp.EXP_CODES.EXP_???, this.ToString());
        // Exception Codes
        public enum EXP_CODES
        {
            EXP_OK = 0,
            EXP_UNKNOWN = 17000,
            EXP_CONFIG = 17001,
            EXP_NOMATCH = 17002,
            EXP_REQFIELD = 17003,
            EXP_NODATA = 17004,
            EXP_DUPDATA = 17005,
            EXP_OUTRANGE = 17006,
            EXP_NOREF = 17007,
            EXP_TRANS = 17008,
            EXP_PREG = 17009,
            EXP_MAX_CALLS = 17101,
            EXP_TS_FAIL = 17201,
            EXP_TS_SIZE = 17202,
            EXP_WEB_GEN = 17301,
            EXP_WEB_NOMATCH = 17302,
            EXP_WEB_ALTKEY = 17303,
            EXP_WEB_NODATA = 17304
        };
        public enum LGN_CODES
        {
            LNG_AMERICAN    // American English
        };

        // Internal State
        private EXP_CODES expCode = EXP_CODES.EXP_OK;
        private string expSource = "";
        private string expDetail = "No Detail Available";

        // Constructors
        public ClassExp(EXP_CODES code, string source)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
        }

        public ClassExp(EXP_CODES code, string source, string detail)
            : base(code.ToString())
        {
            expCode = code;
            expSource = source;
            expDetail = detail;
        }

        // Properties
        public int codeNbr
        {
            get { return (int)expCode; }
        }
        public EXP_CODES code
        {
            get { return expCode; }
        }
        public string codeSource
        {
            get { return expSource; }
        }
        public string codeDesc()
        {
            return codeDesc(LGN_CODES.LNG_AMERICAN);  // Default Language English
        }
        public string codeDesc(LGN_CODES language)
        {
            return desc(language);
        }

        private string desc(LGN_CODES lng)
        {
            string ldesc = "";
            switch (expCode)
            {
                case EXP_CODES.EXP_UNKNOWN:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Unknown or System generated error.";
                    break;
                case EXP_CODES.EXP_CONFIG:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "There was a problem reading some parameters from the configuration file.";
                    break;
                case EXP_CODES.EXP_NOMATCH:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The credentials supplied do not match any on record.";
                    break;
                case EXP_CODES.EXP_REQFIELD:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "A required field is missing data.";
                    break;
                case EXP_CODES.EXP_NODATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The data requested does not seem to be available, please recheck the input values.";
                    break;
                case EXP_CODES.EXP_DUPDATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The data to be created or changed already exists in the system and does not allow duplicate entries.";
                    break;
                case EXP_CODES.EXP_OUTRANGE:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The input data is outside the range of allowable values or is too large to be processed by the system.";
                    break;
                case EXP_CODES.EXP_NOREF:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Reference Table requested does not exist.  Check that the application was installed correctly.";
                    break;
                case EXP_CODES.EXP_TRANS:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Unable to perform all the actions needed to complete the transaction.";
                    break;
                case EXP_CODES.EXP_PREG:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Please properly register the application before using the services.";
                    break;
                case EXP_CODES.EXP_MAX_CALLS:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Maximum allowed calls to the Web Service exceeded.";
                    break;
                case EXP_CODES.EXP_TS_FAIL:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Table Storage unable to process request.";
                    break;
                case EXP_CODES.EXP_TS_SIZE:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "Table Storage unable to store data, it is too large.";
                    break;
                case EXP_CODES.EXP_WEB_GEN:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request to the external web service failed.";
                    break;
                case EXP_CODES.EXP_WEB_NOMATCH:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request query found no match.";
                    break;
                case EXP_CODES.EXP_WEB_ALTKEY:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request used an alternate key.";
                    break;
                case EXP_CODES.EXP_WEB_NODATA:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "The Web Request returned an error.";
                    break;
                default:
                    if (lng.Equals(LGN_CODES.LNG_AMERICAN)) ldesc = "No matching description for error.";
                    break;
            }
            return ldesc + " - " + expDetail;
        }
    }
}
