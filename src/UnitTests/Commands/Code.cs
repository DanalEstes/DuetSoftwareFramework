﻿using DuetAPI.Commands;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Commands
{
    [TestFixture]
    public class Code
    {
        [Test]
        public void ParseG28()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G28 X Y");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(28, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('X', code.Parameters[0].Letter);
            Assert.AreEqual('Y', code.Parameters[1].Letter);
        }

        [Test]
        public void ParseG29()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G29 S1 ; load heightmap");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(29, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(1, code.Parameters.Count);
            Assert.AreEqual('S', code.Parameters[0].Letter);
            Assert.AreEqual(1, (int)code.Parameter('S', 0));
        }

        [Test]
        public void ParseG53()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G53");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(53, code.MajorNumber);
            Assert.IsNull(code.MinorNumber);
        }

        [Test]
        public void ParseG53Line()
        {
            DuetControlServer.Commands.SimpleCode simpleCode = new DuetControlServer.Commands.SimpleCode { Code = "G53 G1 X100 G0 Y200\nG1 Z50" };
            IList<DuetControlServer.Commands.Code> codes = simpleCode.Parse().ToList();
            Assert.AreEqual(3, codes.Count);

            Assert.AreEqual(1, codes[0].MajorNumber);
            Assert.AreEqual(1, codes[0].Parameters.Count);
            Assert.AreEqual('X', codes[0].Parameters[0].Letter);
            Assert.AreEqual(100, (int)codes[0].Parameters[0]);
            Assert.IsTrue(codes[0].Flags.HasFlag(CodeFlags.EnforceAbsolutePosition));

            Assert.AreEqual(0, codes[1].MajorNumber);
            Assert.AreEqual(1, codes[1].Parameters.Count);
            Assert.AreEqual('Y', codes[1].Parameters[0].Letter);
            Assert.AreEqual(200, (int)codes[1].Parameters[0]);
            Assert.IsTrue(codes[1].Flags.HasFlag(CodeFlags.EnforceAbsolutePosition));

            Assert.AreEqual(1, codes[2].MajorNumber);
            Assert.AreEqual(1, codes[2].Parameters.Count);
            Assert.AreEqual('Z', codes[2].Parameters[0].Letter);
            Assert.AreEqual(50, (int)codes[2].Parameters[0]);
            Assert.IsFalse(codes[2].Flags.HasFlag(CodeFlags.EnforceAbsolutePosition));
        }

        [Test]
        public void ParseG54()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G54.6");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(54, code.MajorNumber);
            Assert.AreEqual(6, code.MinorNumber);
        }

        // FIXME: Make quotes for string mandatory and interpret this correctly --v
        [Test]
        public void ParseG92()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G92 XYZ");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(92, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);

            Assert.AreEqual(3, code.Parameters.Count);

            Assert.AreEqual('X', code.Parameters[0].Letter);
            Assert.AreEqual(0, (int)code.Parameters[0]);
            Assert.AreEqual('Y', code.Parameters[1].Letter);
            Assert.AreEqual(0, (int)code.Parameters[1]);
            Assert.AreEqual('Z', code.Parameters[2].Letter);
            Assert.AreEqual(0, (int)code.Parameters[2]);
        }

        [Test]
        public void ParseM32()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M32 some fancy  file.g");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(32, code.MajorNumber);
            Assert.AreEqual("some fancy  file.g", code.GetUnprecedentedString());
        }

        [Test]
        public void ParseM92()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M92 E810:810:407:407");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(92, code.MajorNumber);

            Assert.AreEqual(1, code.Parameters.Count);

            int[] steps = { 810, 810, 407, 407 };
            Assert.AreEqual(steps, (int[])code.Parameter('E'));
        }

        [Test]
        public void ParseM98()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M98 P\"config.g\"");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(98, code.MajorNumber);
            Assert.IsNull(code.MinorNumber);
            Assert.AreEqual(1, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            Assert.AreEqual("config.g", (string)code.Parameters[0]);
        }

        [Test]
        public void ParseM106()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M106 P1 C\"Fancy \"\" Fan\" H-1 S0.5");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(106, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(4, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            Assert.AreEqual(1, (int)code.Parameters[0]);
            Assert.AreEqual('C', code.Parameters[1].Letter);
            Assert.AreEqual("Fancy \" Fan", (string)code.Parameters[1]);
            Assert.AreEqual('H', code.Parameters[2].Letter);
            Assert.AreEqual(-1, (int)code.Parameters[2]);
            Assert.AreEqual('S', code.Parameters[3].Letter);
            Assert.AreEqual(0.5, code.Parameters[3], 0.0001);

            TestContext.Out.Write(JsonConvert.SerializeObject(code, Formatting.Indented));
        }

        [Test]
        public void ParseM563()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M563 P0 D0:1 H1:2                             ; Define tool 0");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(563, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(3, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            Assert.AreEqual(0, (int)code.Parameters[0]);
            Assert.AreEqual('D', code.Parameters[1].Letter);
            Assert.AreEqual(new int[] { 0, 1 }, (int[])code.Parameters[1]);
            Assert.AreEqual('H', code.Parameters[2].Letter);
            Assert.AreEqual(new int[] { 1, 2 }, (int[])code.Parameters[2]);
            Assert.AreEqual(" Define tool 0", code.Comment);
        }

        [Test]
        public void ParseM569()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M569 P1.2 S1 T0.5");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(569, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(CodeFlags.None, code.Flags);
            Assert.AreEqual(3, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            uint driverId = (uint)((1 << 16) | 2);
            Assert.AreEqual(driverId, (uint)code.Parameters[0]);
            Assert.AreEqual('S', code.Parameters[1].Letter);
            Assert.AreEqual(1, (int)code.Parameters[1]);
            Assert.AreEqual('T', code.Parameters[2].Letter);
            Assert.AreEqual(0.5, code.Parameters[2], 0.0001);
        }

        [Test]
        public void ParseM574()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M574 Y2 S1 P\"io1.in\";comment");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(574, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(CodeFlags.None, code.Flags);
            Assert.AreEqual(3, code.Parameters.Count);
            Assert.AreEqual('Y', code.Parameters[0].Letter);
            Assert.AreEqual(2, (int)code.Parameters[0]);
            Assert.AreEqual('S', code.Parameters[1].Letter);
            Assert.AreEqual(1, (int)code.Parameters[1]);
            Assert.AreEqual('P', code.Parameters[2].Letter);
            Assert.AreEqual("io1.in", (string)code.Parameters[2]);
            Assert.AreEqual("comment", code.Comment);
        }

        [Test]
        public void ParseM915()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M915 P2:0.3:1.4 S22");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(915, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(CodeFlags.None, code.Flags);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            uint[] driverIds = new uint[] { 2, 3, (1 << 16) | 4 };
            Assert.AreEqual(driverIds, (uint[])code.Parameters[0]);
            Assert.AreEqual('S', code.Parameters[1].Letter);
            Assert.AreEqual(22, (int)code.Parameters[1]);
        }

        [Test]
        public void ParseT3()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("T3 P4 S\"foo\"");
            Assert.AreEqual(CodeType.TCode, code.Type);
            Assert.AreEqual(3, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(CodeFlags.None, code.Flags);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            Assert.AreEqual(4, (int)code.Parameters[0]);
            Assert.AreEqual('S', code.Parameters[1].Letter);
            Assert.AreEqual("foo", (string)code.Parameters[1]);
            Assert.AreEqual("T3 P4 S\"foo\"", code.ToString());
        }

        [Test]
        public void ParseAbsoluteG1()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G53 G1 X3 Y1.25");
            Assert.AreEqual(CodeFlags.EnforceAbsolutePosition, code.Flags);
            Assert.AreEqual(1, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('X', code.Parameters[0].Letter);
            Assert.AreEqual(3, (int)code.Parameters[0]);
            Assert.AreEqual('Y', code.Parameters[1].Letter);
            Assert.AreEqual(1.25, code.Parameters[1], 0.0001);
        }

        [Test]
        public void ParseQuotedM32()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M32 \"foo bar.g\"");
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(32, code.MajorNumber);
            Assert.AreEqual("foo bar.g", code.GetUnprecedentedString());
        }

        [Test]
        public void ParseUnquotedM32()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M32 foo bar.g");
            Assert.AreEqual(0, code.Indent);
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(32, code.MajorNumber);
            Assert.AreEqual("foo bar.g", code.GetUnprecedentedString());
        }

        [Test]
        public void ParseM586WithComment()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code(" \t M586 P2 S0                               ; Disable Telnet");
            Assert.AreEqual(3, code.Indent);
            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(586, code.MajorNumber);
            Assert.AreEqual(null, code.MinorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('P', code.Parameters[0].Letter);
            Assert.AreEqual(2, (int)code.Parameters[0]);
            Assert.AreEqual('S', code.Parameters[1].Letter);
            Assert.AreEqual(0, (int)code.Parameters[1]);
            Assert.AreEqual(" Disable Telnet", code.Comment);
        }

        [Test]
        public void ParseExpression()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("G1 X{machine.axes[0].maximum - 10} Y{machine.axes[1].maximum - 10}");
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(1, code.MajorNumber);
            Assert.IsNull(code.MinorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('X', code.Parameters[0].Letter);
            Assert.AreEqual("{machine.axes[0].maximum - 10}", (string)code.Parameters[0]);
            Assert.AreEqual('Y', code.Parameters[1].Letter);
            Assert.AreEqual("{machine.axes[1].maximum - 10}", (string)code.Parameters[1]);
        }

        [Test]
        public void ParseLineNumber()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("  N123 G1 X5 Y3");
            Assert.AreEqual(2, code.Indent);
            Assert.AreEqual(123, code.LineNumber);
            Assert.AreEqual(CodeType.GCode, code.Type);
            Assert.AreEqual(1, code.MajorNumber);
            Assert.IsNull(code.MinorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('X', code.Parameters[0].Letter);
            Assert.AreEqual(5, (int)code.Parameters[0]);
            Assert.AreEqual('Y', code.Parameters[1].Letter);
            Assert.AreEqual(3, (int)code.Parameters[1]);
        }

        [Test]
        public void ParseKeywords()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("  if machine.tool.is.great <= 0.03 (some nice) ; comment");
            Assert.AreEqual(2, code.Indent);
            Assert.AreEqual(KeywordType.If, code.Keyword);
            Assert.AreEqual("machine.tool.is.great <= 0.03", code.KeywordArgument);
            Assert.AreEqual("some nice comment", code.Comment);

            code = new DuetAPI.Commands.Code("  elif true");
            Assert.AreEqual(KeywordType.ElseIf, code.Keyword);
            Assert.AreEqual("true", code.KeywordArgument);

            code = new DuetAPI.Commands.Code("  else");
            Assert.AreEqual(KeywordType.Else, code.Keyword);
            Assert.IsNull(code.KeywordArgument);

            code = new DuetAPI.Commands.Code("  while machine.autocal.stddev > 0.04");
            Assert.AreEqual(KeywordType.While, code.Keyword);
            Assert.AreEqual("machine.autocal.stddev > 0.04", code.KeywordArgument);

            code = new DuetAPI.Commands.Code("    break 3");
            Assert.AreEqual(4, code.Indent);
            Assert.AreEqual(KeywordType.Break, code.Keyword);
            Assert.AreEqual("3", code.KeywordArgument);

            code = new DuetAPI.Commands.Code("    return");
            Assert.AreEqual(4, code.Indent);
            Assert.AreEqual(KeywordType.Return, code.Keyword);
            Assert.IsEmpty(code.KeywordArgument);

            code = new DuetAPI.Commands.Code("    abort foo bar");
            Assert.AreEqual(4, code.Indent);
            Assert.AreEqual(KeywordType.Abort, code.Keyword);
            Assert.AreEqual("foo bar", code.KeywordArgument);

            code = new DuetAPI.Commands.Code("  var asdf=0.34");
            Assert.AreEqual(2, code.Indent);
            Assert.AreEqual(KeywordType.Var, code.Keyword);
            Assert.AreEqual("asdf=0.34", code.KeywordArgument);

            code = new DuetAPI.Commands.Code("  set asdf=\"meh\"");
            Assert.AreEqual(2, code.Indent);
            Assert.AreEqual(KeywordType.Set, code.Keyword);
            Assert.AreEqual("asdf=\"meh\"", code.KeywordArgument);
        }

        [Test]
        public void ParseMultipleCodesSpace()
        {
            DuetControlServer.Commands.SimpleCode simpleCode = new DuetControlServer.Commands.SimpleCode { Code = "G91 G1 X5 Y2" };
            IList<DuetControlServer.Commands.Code> codes = simpleCode.Parse().ToList();

            Assert.AreEqual(2, codes.Count);

            Assert.AreEqual(CodeType.GCode, codes[0].Type);
            Assert.AreEqual(91, codes[0].MajorNumber);

            Assert.AreEqual(CodeType.GCode, codes[1].Type);
            Assert.AreEqual(1, codes[1].MajorNumber);
            Assert.AreEqual(2, codes[1].Parameters.Count);
            Assert.AreEqual('X', codes[1].Parameters[0].Letter);
            Assert.AreEqual(5, (int)codes[1].Parameters[0]);
            Assert.AreEqual('Y', codes[1].Parameters[1].Letter);
            Assert.AreEqual(2, (int)codes[1].Parameters[1]);
        }

        [Test]
        public void ParseMultipleCodesNL()
        {
            DuetControlServer.Commands.SimpleCode simpleCode = new DuetControlServer.Commands.SimpleCode { Code = "G91\nG1 X5 Y2" };
            IList<DuetControlServer.Commands.Code> codes = simpleCode.Parse().ToList();

            Assert.AreEqual(2, codes.Count);

            Assert.AreEqual(CodeType.GCode, codes[0].Type);
            Assert.AreEqual(91, codes[0].MajorNumber);

            Assert.AreEqual(CodeType.GCode, codes[1].Type);
            Assert.AreEqual(1, codes[1].MajorNumber);
            Assert.AreEqual(2, codes[1].Parameters.Count);
            Assert.AreEqual('X', codes[1].Parameters[0].Letter);
            Assert.AreEqual(5, (int)codes[1].Parameters[0]);
            Assert.AreEqual('Y', codes[1].Parameters[1].Letter);
            Assert.AreEqual(2, (int)codes[1].Parameters[1]);
        }

        [Test]
        public void ParseCompactCode()
        {
            DuetAPI.Commands.Code code = new DuetAPI.Commands.Code("M302D\"dummy\"P1");

            Assert.AreEqual(CodeType.MCode, code.Type);
            Assert.AreEqual(302, code.MajorNumber);
            Assert.AreEqual(2, code.Parameters.Count);
            Assert.AreEqual('D', code.Parameters[0].Letter);
            Assert.AreEqual("dummy", (string)code.Parameters[0]);
            Assert.AreEqual('P', code.Parameters[1].Letter);
            Assert.AreEqual(1, (int)code.Parameters[1]);
        }
    }
}
