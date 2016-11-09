using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("Check time", "Date / Time", "Check if the current date/time match the specified criteria")]
    public class CheckTime : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return year;
                yield return month;
                yield return day;
                yield return hour;
                yield return minute;
                yield return second;
            }
        }

        private Variable<NumberPattern> year = new Variable<NumberPattern>("Year", NumberPattern.All);
        private Variable<NumberPattern> month = new Variable<NumberPattern>("Month", NumberPattern.All);
        private Variable<NumberPattern> day = new Variable<NumberPattern>("Day", NumberPattern.All);
        private Variable<NumberPattern> hour = new Variable<NumberPattern>("Hour", NumberPattern.All);
        private Variable<NumberPattern> minute = new Variable<NumberPattern>("Minute", NumberPattern.All);
        private Variable<NumberPattern> second = new Variable<NumberPattern>("Second", NumberPattern.All);
        
        public override NodeStep Evaluate()
        {
            DateTime now = DateTime.Now;

            if (year.Value.Check(now.Year) && month.Value.Check(now.Month) && day.Value.Check(now.Day) && hour.Value.Check(now.Hour) && minute.Value.Check(now.Minute) && second.Value.Check(now.Second))
                return new NodeStep(NodeResult.Success, TrueSlot);
            else
                return new NodeStep(NodeResult.Success, FalseSlot);
        }
    }
}