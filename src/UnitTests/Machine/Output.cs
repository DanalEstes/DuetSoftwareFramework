﻿using DuetAPI;
using DuetAPI.Machine;
using NUnit.Framework;

namespace UnitTests.Machine
{
    [TestFixture]
    public class Output
    {
        [Test]
        public void Clone()
        {
            MachineModel original = new MachineModel();

            original.Messages.Add(new Message(MessageType.Warning, "Test 1 2 3"));
            original.Messages.Add(new Message(MessageType.Error, "Err 3 2 1"));

            MachineModel clone = (MachineModel)original.Clone();

            Assert.AreEqual(2, clone.Messages.Count);
            Assert.AreEqual(clone.Messages[0].Content, "Test 1 2 3");
            Assert.AreEqual(clone.Messages[0].Type, MessageType.Warning);
            Assert.AreEqual(clone.Messages[1].Content, "Err 3 2 1");
            Assert.AreEqual(clone.Messages[1].Type, MessageType.Error);
        }

        [Test]
        public void Assign()
        {
            MachineModel original = new MachineModel();

            original.Messages.Add(new Message(MessageType.Warning, "Test 1 2 3"));
            original.Messages.Add(new Message(MessageType.Error, "Err 3 2 1"));

            MachineModel assigned = new MachineModel();
            assigned.Assign(original);

            Assert.AreEqual(2, original.Messages.Count);
            Assert.AreEqual(original.Messages[0].Content, "Test 1 2 3");
            Assert.AreEqual(original.Messages[0].Type, MessageType.Warning);
            Assert.AreEqual(original.Messages[1].Content, "Err 3 2 1");
            Assert.AreEqual(original.Messages[1].Type, MessageType.Error);
        }
    }
}
