using Autodesk.Revit.DB;
using System;
using System.Diagnostics;
using System.Linq;
using GlobalParameter = Autodesk.Revit.DB.GlobalParameter;


namespace IBIMTool.RevitExtensions
{
    public static class ParameterExtention
    {

        public static string RealString(double arg)
        {
            return arg.ToString("0.##");
        }


        public static string GetParameterType(this Parameter parameter)
        {
            ParameterType prt = parameter.Definition.ParameterType;
            string srp = ParameterType.Invalid == prt ? "" : "/" + prt;
            return parameter.StorageType + srp;
        }


        public static bool SetParamValueByName(this FamilyInstance inst, in string name, in object value)
        {
            if (!string.IsNullOrWhiteSpace(name) && value != null)
            {
                Parameter prm = inst.GetParameters(name).FirstOrDefault();
                if (prm != null && prm is Parameter parameter)
                {
                    StorageType storage = parameter.StorageType;
                    if (storage == StorageType.Double && value is double dblval)
                    {
                        return parameter.Set(dblval);
                    }

                    if (storage == StorageType.String && value is string strval)
                    {
                        return parameter.Set(strval);
                    }

                    if (storage == StorageType.Integer && value is int intval)
                    {
                        return parameter.Set(intval);
                    }
                }
                else
                {
                    throw new Exception("Invalid parameter: " + name);
                }
            }
            return false;
        }


        public static string GetValue(this Parameter parameter)
        {
            string value;
            switch (parameter.StorageType)
            {
                // database value, internal revitUnits, e.g. feet:
                case StorageType.Double:
                    value = RealString(parameter.AsDouble());
                    break;
                case StorageType.Integer:
                    value = parameter.AsInteger().ToString();
                    break;
                case StorageType.String:
                    value = parameter.AsString();
                    break;
                case StorageType.ElementId:
                    value = parameter.AsElementId().IntegerValue.ToString();
                    break;
                case StorageType.None:
                    value = "None";
                    break;
                default:
                    value = string.Empty;
                    Debug.Assert(false, "unexpected storage type");
                    break;
            }
            return value;
        }


        public static Parameter GetParameter(this Element elem, string name)
        {
            return elem.GetParameters(name).FirstOrDefault();
        }


        /// <summary>
        /// IBIMToolHelper to return parameter value as string, with additional
        /// support for instance instanceId to display the instance stype referred to.
        /// </summary>
        public static string GetParameterValueByDocument(this Parameter param, Document doc)
        {
            if (param.StorageType == StorageType.ElementId)
            {
                ElementId paramId = param.AsElementId();
                Element element = doc.GetElement(paramId);
                return element.Name;
            }
            else
            {
                return param.GetStringValue();
            }
        }


        /// <summary>
        /// Helper to return parameter value as string.
        /// One can also use parameter.AsValueString() to
        /// get the user interface representation.
        /// </summary>
        public static string GetStringValue(this Parameter param)
        {
            string parameterString = param.StorageType switch
            {
                StorageType.String => param.AsString(),
                StorageType.Double => Convert.ToString(param.AsDouble()),
                StorageType.Integer => Convert.ToString(param.AsInteger()),
                StorageType.ElementId => Convert.ToString(param.AsElementId()),
                _ => throw new NotImplementedException(param.Definition.Name),
            };
            return parameterString;
        }


        /// <summary>
        /// Return Guid Of SelectedParameter Share
        /// </summary>
        public static string Guid(this Parameter parameter)
        {
            return parameter.IsShared ? parameter.GUID.ToString() : string.Empty;
        }


        /// <summary>
        /// Return Global SelectedParameter SymbolName
        /// </summary>
        public static string GetAssGlobalParameter(this Parameter parameter, Document doc)
        {
            ElementId elementId = parameter.GetAssociatedGlobalParameter();
            if (elementId != null)
            {
                if (doc.GetElement(elementId) is GlobalParameter globalParameter)
                {
                    return globalParameter.GetDefinition().Name;
                }
            }
            return string.Empty;
        }


        /// <summary>
        /// Return Global SelectedParameter Content
        /// </summary>
        public static string GetAssGlobalParameterValue(this Parameter parameter, Document doc)
        {
            ElementId elementId = parameter.GetAssociatedGlobalParameter();
            if (elementId != null)
            {
                if (doc.GetElement(elementId) is GlobalParameter globalParameter)
                {
                    object value = globalParameter.GetValue();
                    if (value is DoubleParameterValue dblprmval)
                    {
                        return RealString(dblprmval.Value);
                    }
                    if (value is StringParameterValue strprmval)
                    {
                        return strprmval.Value;
                    }
                }
            }
            return string.Empty;
        }


        public static double ConvertFromInternalUnits(double value, string displayUnitType)
        {
            if (string.IsNullOrWhiteSpace(displayUnitType))
            {
                throw new ArgumentNullException(nameof(displayUnitType));
            }

            DisplayUnitType dut = (DisplayUnitType)Enum.Parse(typeof(DisplayUnitType), displayUnitType);
            double result = UnitUtils.ConvertFromInternalUnits(value, dut);

            return result;
        }





        public static bool SetValue(this Parameter param, object value)
        {
            bool result = false;
            StorageType stype = param.StorageType;
            if (!param.IsReadOnly && value != null)
            {
                if (stype == StorageType.None)
                {
                    result = false;
                }
                else if (stype == StorageType.String)
                {
                    result = value is string strVal ? param.Set(strVal) : param.Set(Convert.ToString(value));
                }
                else if (stype == StorageType.Double)
                {
                    if (value is double val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (double.TryParse(strVal, out double dblval))
                        {
                            result = param.Set(dblval);
                        }
                    }
                    else
                    {
                        result = param.Set(Convert.ToDouble(value));
                    }
                }
                else if (stype == StorageType.Integer)
                {
                    if (value is int val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (int.TryParse(strVal, out int intval))
                        {
                            result = param.Set(intval);
                        }
                    }
                    else
                    {
                        result = param.Set(Convert.ToInt16(value));
                    }
                }
                else if (stype == StorageType.ElementId)
                {
                    if (value is ElementId val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (int.TryParse(strVal, out int idval))
                        {
                            result = param.Set(new ElementId(idval));
                        }
                    }
                }
            }
            return result;
        }


        //var prm = SharedParameterElement.Lookup(doc, guid);

    }
}
