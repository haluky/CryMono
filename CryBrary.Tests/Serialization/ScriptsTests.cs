﻿using CryEngine;
using CryEngine.Serialization;
using CryEngine.Initialization;

using NUnit.Framework;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CryBrary.Tests.Serialization
{
    [TestFixture]
	public class ScriptsTests : CryBraryTests
	{
		[Test]
		public void Serialize_Native_Scripts()
		{
			var serializer = new CrySerializer();

			using(var stream = new MemoryStream())
			{
				var scriptManager = new ScriptManager();

				scriptManager.AddScriptInstance(new NativeEntity(1, IntPtr.Zero), ScriptType.Entity);
				scriptManager.AddScriptInstance(new NativeEntity(2, IntPtr.Zero), ScriptType.Entity);
				scriptManager.AddScriptInstance(new NativeActor(3), ScriptType.Actor);

				serializer.Serialize(stream, scriptManager.Scripts);

				scriptManager.Scripts = serializer.Deserialize(stream) as List<CryScript>;
				Assert.IsNotNull(scriptManager.Scripts);
				Assert.AreEqual(2, scriptManager.Scripts.Count);

				var entityScript = scriptManager.FindScript(ScriptType.Entity, x => x.Type == typeof(NativeEntity));
				Assert.AreNotSame(default(CryScript), entityScript);
				Assert.IsNotNull(entityScript.ScriptInstances);
				Assert.AreEqual(1, (entityScript.ScriptInstances[0] as EntityBase).Id);
				Assert.AreEqual(2, (entityScript.ScriptInstances[1] as EntityBase).Id);

				var actor = scriptManager.Find<NativeActor>(ScriptType.Actor, x => x.Id == 3);
				Assert.IsNotNull(actor);
			}
		}
        
        [Test]
        public void Serialize_CryScript_List()
        {
            var serializer = new CrySerializer();

            using (var stream = new MemoryStream())
            {
                var list = new List<CryScript>();

				CryScript script;
				if (CryScript.TryCreate(typeof(NativeActor), out script))
				{
					script.ScriptInstances = new List<CryScriptInstance>();
					script.ScriptInstances.Add(new NativeActor(759));
					script.ScriptInstances.Add(new NativeActor(5));

					list.Add(script);
				}

				if (CryScript.TryCreate(typeof(NativeEntity), out script))
				{
					script.ScriptInstances = new List<CryScriptInstance>();
					script.ScriptInstances.Add(new NativeEntity(987, IntPtr.Zero));
					script.ScriptInstances.Add(new NativeEntity(8, IntPtr.Zero));
					script.ScriptInstances.Add(null);

					list.Add(script);
				}

                serializer.Serialize(stream, list);

                serializer = new CrySerializer();
                list = serializer.Deserialize(stream) as List<CryScript>;

                Assert.IsNotNull(list);
                Assert.IsNotEmpty(list);

                Assert.AreEqual(2, list.Count);

                var nativeActorScript = list.ElementAt(0);
                Assert.IsNotNull(nativeActorScript.ScriptInstances);
                Assert.AreEqual(2, nativeActorScript.ScriptInstances.Count);

                Assert.IsNotNull(nativeActorScript.ScriptInstances.ElementAt(0));
                Assert.AreEqual(759, (nativeActorScript.ScriptInstances.ElementAt(0) as EntityBase).Id);

                Assert.IsNotNull(nativeActorScript.ScriptInstances.ElementAt(1));
                Assert.AreEqual(5, (nativeActorScript.ScriptInstances.ElementAt(1) as EntityBase).Id);

               var  nativeEntityScript = list.ElementAt(1);

                Assert.IsNotNull(nativeEntityScript.ScriptInstances);
                Assert.AreEqual(3, nativeEntityScript.ScriptInstances.Count);

                Assert.IsNotNull(nativeEntityScript.ScriptInstances.ElementAt(0));
                Assert.AreEqual(987, (nativeEntityScript.ScriptInstances.ElementAt(0) as EntityBase).Id);

                Assert.IsNotNull(nativeEntityScript.ScriptInstances.ElementAt(1));
                Assert.AreEqual(8, (nativeEntityScript.ScriptInstances.ElementAt(1) as EntityBase).Id);

                Assert.IsNull(nativeEntityScript.ScriptInstances.ElementAt(2));
            }
        }

        [Test]
        public void Serialize_DelayedFunc_List()
        {
            var serializer = new CrySerializer();

            using (var stream = new MemoryStream())
            {
                var list = new List<DelayedFunc>();
                list.Add(new DelayedFunc(DelayedMethod, 1500));
                list.Add(new DelayedFunc(() => { }, 2400));

                serializer.Serialize(stream, list);

                list = serializer.Deserialize(stream) as List<DelayedFunc>;
                Assert.IsNotNull(list);
                Assert.IsNotEmpty(list);
                Assert.AreEqual(2, list.Count);

                Assert.AreEqual(1500, list.ElementAt(0).Delay);
                Assert.AreEqual(2400, list.ElementAt(1).Delay);
            }
        }

        public void DelayedMethod() { }

        class TestEntity : Entity
        {
            public TestEntity() { delayedFunc = new DelayedFunc(MyDelayedFunc, 1337); }
            void MyDelayedFunc() { }

            DelayedFunc delayedFunc;
        }

        [Test]
        public void Entity_With_DelayedFunc()
        {
            var serializer = new CrySerializer();

            using (var stream = new MemoryStream())
            {
				var scriptManager = new ScriptManager();

				scriptManager.AddScriptInstance(new TestEntity(), ScriptType.Entity);

				serializer.Serialize(stream, scriptManager.Scripts);

				scriptManager.Scripts = serializer.Deserialize(stream) as List<CryScript>;
            }
        }
	}
}
