using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace STK.Tools.Test
{
    [TestClass]
    public class SettingsManagerTest
    {
        [TestMethod]
        public void SaveAndLoad()
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();

            vals.Add("a", "A");
            vals.Add("ab", "A");
            vals.Add("abc", 1);
            vals.Add("abcd", 2);

            SettingsManager sm = new SettingsManager(vals, "test");
            sm.Save("test");

            SettingsManager sm2 = new SettingsManager(vals, "test");
            sm2.Load(sm.CurrentLoaded);

            Assert.IsTrue("A".CompareTo(sm2.Get<string>("a")) == 0);
            Assert.IsTrue("A".CompareTo(sm2.Get<string>("ab")) == 0);
            Assert.IsTrue(1 == sm2.Get<int>("abc"));
            Assert.IsTrue(2 == sm2.Get<int>("abcd"));

            SettingsManager sm3 = new SettingsManager(vals, "test");
            sm3.Load(sm.CurrentLoaded);
            sm3.Set("ab", "B");

            Assert.IsTrue("A".CompareTo(sm3.Get<string>("a")) == 0);
            Assert.IsTrue("B".CompareTo(sm3.Get<string>("ab")) == 0);
            Assert.IsTrue(1 == sm3.Get<int>("abc"));
            Assert.IsTrue(2 == sm3.Get<int>("abcd"));
        }

        [TestMethod]
        public void SetAndGet()
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();

            vals.Add("a", "A");
            vals.Add("ab", "A");
            vals.Add("abc", 1);
            vals.Add("abcd", 2);

            SettingsManager sm = new SettingsManager(vals, "test");

            ArgumentException ex = null;
            try
            {
                sm.Set("a", 1);
            }
            catch (ArgumentException e)
            {
                e = ex;
            }
            Assert.IsNull(ex, "Inconclusive type set should not be possible");

            sm.Set("a", "B");
            Assert.IsTrue("B".CompareTo(sm.Get<string>("a")) == 0);
        }
    }
}
