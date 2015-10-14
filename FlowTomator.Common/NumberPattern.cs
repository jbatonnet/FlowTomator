using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [TypeConverter(typeof(NumberPatternConverter))]
    public struct NumberPattern
    {
        public struct Range
        {
            public static Range All
            {
                get
                {
                    return new Range(uint.MinValue, uint.MaxValue, 1);
                }
            }

            private static Regex regex = new Regex(@"^(?:(\*)|(\d+)(?:-(\d+))?)(?:\/(\d+))?$", RegexOptions.Compiled);

            public uint From { get; private set; }
            public uint To { get; private set; }
            public uint Divider { get; private set; }

            public Range(uint from, uint to, uint divider)
            {
                From = from;
                To = to;
                Divider = divider;
            }

            public override string ToString()
            {
                string result = "";

                if (From == uint.MinValue && To == uint.MaxValue)
                    result = "*";
                else if (From == To)
                    result = From.ToString();
                else
                    result = From + "-" + To;

                if (Divider == 1)
                    return result;
                else
                    return result + "/" + Divider;
            }

            public static Range Parse(string text)
            {
                Match match = regex.Match(text);

                if (!match.Success)
                    throw new FormatException("Could not parse specified number pattern");

                bool token = match.Groups[1].Success;
                uint divider = match.Groups[4].Success ? uint.Parse(match.Groups[4].Value) : 1;
                uint from, to;

                if (!token && match.Groups[2].Success)
                {
                    from = uint.Parse(match.Groups[2].Value);

                    if (match.Groups[3].Success)
                        to = uint.Parse(match.Groups[3].Value);
                    else
                        to = from;
                }
                else
                {
                    from = uint.MinValue;
                    to = uint.MaxValue;
                }

                return new Range(from, to, divider);
            }
        }

        public static NumberPattern All
        {
            get
            {
                return new NumberPattern(Range.All);
            }
        }

        public Range[] Ranges { get; private set; }

        public NumberPattern(params Range[] ranges)
        {
            Ranges = ranges;
        }

        public bool Check(int value)
        {
            foreach (Range range in Ranges)
                if (value >= range.From && value <= range.To && (value - range.From) % range.Divider == 0)
                    return true;

            return false;
        }
        public override string ToString()
        {
            return string.Join(",", Ranges);
        }
    }
    public class NumberPatternConverter : TypeConverter
    {
        private static Regex globalRegex = new Regex(@"^((?:\*|\d+(?:-\d+)?)(?:\/\d+)?)(?:,((?:\*|\d+(?:-\d+)?)(?:\/\d+)?))*$", RegexOptions.Compiled);

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                Match match = globalRegex.Match(value as string);

                if (!match.Success)
                    throw new FormatException("Could not parse specified number pattern");

                List<NumberPattern.Range> ranges = new List<NumberPattern.Range>();

                if (match.Groups[1].Success)
                {
                    ranges.Add(NumberPattern.Range.Parse(match.Groups[1].Value));

                    if (match.Groups[2].Success)
                        for (int i = 0; i < match.Groups[2].Captures.Count; i++)
                            ranges.Add(NumberPattern.Range.Parse(match.Groups[2].Captures[i].Value));
                }

                return new NumberPattern(ranges.ToArray());
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}