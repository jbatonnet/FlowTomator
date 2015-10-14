using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common.Nodes
{
    [Node("Timed event", "Date / Time", "Triggers the following nodes when the specified date and time conditions are met")]
    public class TimedEvent : Event
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                return new[] { year, month, day, hour, minute, second };
            }
        }

        private Variable<NumberPattern> year = new Variable<NumberPattern>("Year", NumberPattern.All);
        private Variable<NumberPattern> month = new Variable<NumberPattern>("Month", NumberPattern.All);
        private Variable<NumberPattern> day = new Variable<NumberPattern>("Day", NumberPattern.All);
        private Variable<NumberPattern> hour = new Variable<NumberPattern>("Hour", NumberPattern.All);
        private Variable<NumberPattern> minute = new Variable<NumberPattern>("Minute", NumberPattern.All);
        private Variable<NumberPattern> second = new Variable<NumberPattern>("Second", NumberPattern.All);

        private DateTime nextUpdate;

        public override NodeResult Check()
        {
            DateTime end;

            nextUpdate = DateTime.Now;
            nextUpdate = nextUpdate.AddMilliseconds(1000 - nextUpdate.Millisecond);

            if (Timeout == TimeSpan.MaxValue)
                end = DateTime.MaxValue;
            else
                end = DateTime.Now + Timeout;

            while (true)
            {
                DateTime now = DateTime.Now;

                if (now < nextUpdate)
                {
                    Thread.Sleep(500);
                    continue;
                }
                if (now > end)
                    return NodeResult.Skip;

                if (year.Value.Check(nextUpdate.Year) && month.Value.Check(nextUpdate.Month) && day.Value.Check(nextUpdate.Day) && hour.Value.Check(nextUpdate.Hour) && minute.Value.Check(nextUpdate.Minute) && second.Value.Check(nextUpdate.Second))
                    return NodeResult.Success;

                nextUpdate = nextUpdate.AddSeconds(1);
            }
        }
    }
}