﻿using System;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using System.ComponentModel;
using System.Reflection;

using System.Linq;

using CryEngine.Extensions;

namespace CryEngine
{
	/// <summary>
	/// The base class for all entities in the game world.
	/// </summary>
	public partial class Entity : FlowNode
	{
		#region Externals
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static string _GetPropertyValue(uint entityId, string propertyName);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetPropertyValue(uint entityId, string property, string value);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetWorldPos(uint entityId, Vec3 newPos);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static Vec3 _GetWorldPos(uint entityId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetRotation(uint entityId, Quat newAngles);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static Quat _GetRotation(uint entityId);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _LoadObject(uint entityId, string fileName, int slot);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static string _GetStaticObjectFilePath(uint entityId, int slot);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static BoundingBox _GetBoundingBox(uint entityId, int slot);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static EntitySlotFlags _GetSlotFlags(uint entityId, int slot);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetSlotFlags(uint entityId, int slot, EntitySlotFlags slotFlags);

		public EntitySlotFlags GetSlotFlags(int slot = 0)
		{
			return _GetSlotFlags(Id, slot);
		}

		public void SetSlotFlags(EntitySlotFlags flags, int slot = 0)
		{
			_SetSlotFlags(Id, slot, flags);
		}

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetWorldTM(uint entityId, Matrix34 tm);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static Matrix34 _GetWorldTM(uint entityId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetLocalTM(uint entityId, Matrix34 tm);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static Matrix34 _GetLocalTM(uint entityId);

		public Matrix34 WorldTM { get { return _GetWorldTM(Id); } set { _SetWorldTM(Id, value); } }
		public Matrix34 LocalTM { get { return _GetLocalTM(Id); } set { _SetLocalTM(Id, value); } }

		public BoundingBox BoundingBox { get { return _GetBoundingBox(Id, 0); } }

		/// <summary>
		/// Loads an non-static model on the object (.chr, .cdf, .cga)
		/// </summary>
		/// <param name="entityId"></param>
		/// <param name="fileName"></param>
		/// <param name="slot"></param>
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _LoadCharacter(uint entityId, string fileName, int slot);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _CreateGameObjectForEntity(uint entityId);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _AddMovement(uint entityId, ref EntityMovementRequest request);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static Vec3 _GetVelocity(uint entityId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetVelocity(uint entityId, Vec3 velocity);

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static string _GetMaterial(uint entityId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		extern internal static void _SetMaterial(uint entityId, string material);
		#endregion

		public Entity() { }

		internal Entity(EntityId entityId)
		{
			Id = entityId;

			MonoEntity = false;
		}

		/// <summary>
		/// Initializes the entity, not recommended to set manually.
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns>IsEntityFlowNode</returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool InternalSpawn(EntityId entityId)
		{
			SpawnCommon(entityId);
			OnSpawn();

			return IsEntityFlowNode();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual void InternalRemove()
		{
			OnRemove();

			Entity.RemoveInternalEntity(Id);
		}

		/// <summary>
		/// Returns true if this entity contains input or output ports.
		/// </summary>
		/// <returns></returns>
		public bool IsEntityFlowNode()
		{
			var members = GetType().GetMembers(BindingFlags.Instance);
			if(members == null || members.Length <= 0)
				return false;

			return members.Any(member => member.ContainsAttribute<PortAttribute>());
		}

		internal void SpawnCommon(EntityId entityId)
		{
			Id = entityId;

			MonoEntity = true;
			Spawned = true;

			//Do this before the property overwrites
			InitPhysics();

			if(CanContainEditorProperties)
			{
				//TODO: Make sure that mutators are only called once on startup
				//var storedPropertyNames = storedProperties.Keys.Select(key => key[0]);

				foreach(var property in GetType().GetProperties())
				{
					EditorPropertyAttribute attr;
					try
					{
						if(property.TryGetAttribute(out attr) && attr.DefaultValue != null)// && !storedPropertyNames.Contains(property.Name))
							property.SetValue(this, attr.DefaultValue, null);
					}
					catch(Exception ex)
					{
						Debug.LogException(ex);
					}
				}

				foreach(var field in GetType().GetFields())
				{
					EditorPropertyAttribute attr;
					if(field.TryGetAttribute(out attr) && attr.DefaultValue != null)// && !storedPropertyNames.Contains(field.Name))
						field.SetValue(this, attr.DefaultValue);
				}

				if(storedProperties == null)
					return;

				foreach(var storedProperty in storedProperties.Where(prop => !string.IsNullOrEmpty(prop.Key[1])))
					SetPropertyValue(storedProperty.Key[0], storedProperty.Value, storedProperty.Key[1]);

				storedProperties.Clear();
				storedProperties = null;
			}

			Entity.RegisterInternalEntity(this);
		}

		internal virtual bool CanContainEditorProperties { get { return true; } }

		#region Methods & Fields
		public Vec3 Position { get { return _GetWorldPos(Id); } set { _SetWorldPos(Id, value); } }
		public Quat Rotation { get { return _GetRotation(Id); } set { _SetRotation(Id, value); } }

		public EntityId Id { get; set; }
		public string Name { get; set; }
		public EntityFlags Flags { get; set; }
		internal bool Spawned;

		internal bool MonoEntity;
		#endregion

		#region Callbacks
		/// <summary>
		/// This callback is called when this entity has finished spawning. The entity has been created and added to the list of entities.
		/// </summary>
		public virtual void OnSpawn() { }

		/// <summary>
		/// Called when the entity is being removed.
		/// </summary>
		/// <returns>True to allow removal, false to deny.</returns>
		protected virtual bool OnRemove() { return true; }

		/// <summary>
		/// Called when resetting the state of the entity in Editor.
		/// </summary>
		/// <param name="enteringGame">true if currently entering gamemode, false if exiting.</param>
		protected virtual void OnReset(bool enteringGame) { }

		/// <summary>
		/// Called when game is started (games may start multiple times)
		/// </summary>
		protected virtual void OnStartGame() { }

		/// <summary>
		/// Called when the level is started.
		/// </summary>
		protected virtual void OnStartLevel() { }

		/// <summary>
		/// Sent when triggering entity enters to the area proximity.
		/// </summary>
		/// <param name="triggerEntityId"></param>
		/// <param name="areaEntityId"></param>
		protected virtual void OnEnterArea(EntityId triggerEntityId, EntityId areaEntityId) { }

		/// <summary>
		/// Sent when triggering entity leaves the area proximity.
		/// </summary>
		/// <param name="triggerEntityId"></param>
		/// <param name="areaEntityId"></param>
		protected virtual void OnLeaveArea(EntityId triggerEntityId, EntityId areaEntityId) { }

		/// <summary>
		/// Sent on entity collision.
		/// </summary>
		/// <param name="targetEntityId"></param>
		/// <param name="hitPos"></param>
		/// <param name="dir"></param>
		/// <param name="materialId"></param>
		/// <param name="contactNormal"></param>
		protected virtual void OnCollision(EntityId targetEntityId, Vec3 hitPos, Vec3 dir, short materialId, Vec3 contactNormal) { }
		
		/// <summary>
		/// 
		/// </summary>
		public virtual void OnHit(HitInfo hitInfo) { }
		#endregion

		#region Overrides
		// The stash; hide all internal code we don't want anyone to see down here. *sweep sweep*
		public override bool Equals(object obj)
		{
			Entity ent = obj as Entity;

			return ent != null && this.Id == ent.Id;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region Base Logic

		protected virtual string GetPropertyValue(string propertyName)
		{
			return _GetPropertyValue(Id, propertyName);
		}

		/// <summary>
		/// Temporarily stored so we can set them properly on spawn.
		/// </summary>
		Dictionary<string[], EntityPropertyType> storedProperties;

		internal virtual void SetPropertyValue(string propertyName, EntityPropertyType propertyType, string value)
		{
			if(value.Length <= 0 && propertyType != EntityPropertyType.String)
				return;

			// Perhaps we should exclude properties entirely, and just utilize fields (including backing fields)
			var property = GetType().GetProperty(propertyName);
			if(property != null)
			{
				// Store properties so we can utilize the get set functionality after opening a saved level.
				if(!Spawned)
				{
					if(storedProperties == null)
						storedProperties = new Dictionary<string[], EntityPropertyType>();

					storedProperties.Add(new string[] { propertyName, value }, propertyType);

					return;
				}

				property.SetValue(this, Convert.FromString(propertyType, value), null);

				return;
			}

			var field = GetType().GetField(propertyName);
			if(field != null)
				field.SetValue(this, Convert.FromString(propertyType, value));
		}

		/// <summary>
		/// Loads a mesh for this entity. Can optionally load multiple meshes using entity slots.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="slotNumber"></param>
		/// <returns></returns>
		public bool LoadObject(string name, int slotNumber = 0)
		{
			if(name.EndsWith("cgf"))
				_LoadObject(Id, name, slotNumber);
			else if(name.EndsWith("cdf") || name.EndsWith("cga") || name.EndsWith("cga"))
				_LoadCharacter(Id, name, slotNumber);
			else
				return false;

			return true;
		}

		protected string GetObjectFilePath(int slot = 0)
		{
			return _GetStaticObjectFilePath(Id, slot);
		}

		internal static EntityConfig GetEntityConfig(Type type)
		{
			var properties = type.GetProperties();
			var fields = type.GetFields();
			var entityProperties = new List<object>();

			//Process all properties
			foreach(var property in properties)
			{
				if(property.ContainsAttribute<EditorPropertyAttribute>())
				{
					var attribute = property.GetAttribute<EditorPropertyAttribute>();
					EntityPropertyType propertyType = GetEditorType(property.PropertyType, attribute.Type);
					var limits = new EntityPropertyLimits(attribute.Min, attribute.Max);

					entityProperties.Add(new EntityProperty(property.Name, attribute.Description, propertyType, limits, attribute.Flags));
				}
			}

			//Process all fields
			foreach(var field in fields)
			{
				if(field.ContainsAttribute<EditorPropertyAttribute>())
				{
					var attribute = field.GetAttribute<EditorPropertyAttribute>();
					EntityPropertyType propertyType = GetEditorType(field.FieldType, attribute.Type);
					var limits = new EntityPropertyLimits(attribute.Min, attribute.Max);

					entityProperties.Add(new EntityProperty(field.Name, attribute.Description, propertyType, limits, attribute.Flags));
				}
			}

			return new EntityConfig(GetRegistrationConfig(type), entityProperties.ToArray());
		}

		internal static EntityPropertyType GetEditorType(Type type, EntityPropertyType propertyType)
		{
			//If a special type is needed, do this here.
			switch(propertyType)
			{
				case EntityPropertyType.Object:
				case EntityPropertyType.Texture:
				case EntityPropertyType.File:
				case EntityPropertyType.Sound:
				case EntityPropertyType.Dialogue:
				case EntityPropertyType.Sequence:
					{
						if(type == typeof(string))
							return propertyType;
						else
							throw new EntityException("File selector type was specified, but property was not a string.");
					}
				case EntityPropertyType.Color:
					{
						if(type == typeof(Vec3))
							return propertyType;
						else
							throw new EntityException("Vector type was specified, but property was not a vector.");
					}
			}

			//OH PROGRAMMING GODS, FORGIVE ME
			if(type == typeof(string))
				return EntityPropertyType.String;
			else if(type == typeof(int))
				return EntityPropertyType.Int;
			else if(type == typeof(float) || type == typeof(double))
				return EntityPropertyType.Float;
			else if(type == typeof(bool))
				return EntityPropertyType.Bool;
			else if(type == typeof(Vec3))
				return EntityPropertyType.Vec3;
			else
				throw new EntityException("Invalid property type specified.");
		}

		internal override NodeConfig GetNodeConfig()
		{
			return new NodeConfig(FlowNodeCategory.Approved, "", FlowNodeFlags.HideUI | FlowNodeFlags.TargetEntity);
		}

		internal static EntityRegisterParams GetRegistrationConfig(Type type)
		{
			EntityAttribute entityAttribute = type.ContainsAttribute<EntityAttribute>() ? type.GetAttribute<EntityAttribute>() : new EntityAttribute();

			return new EntityRegisterParams(entityAttribute.Name ?? type.Name, entityAttribute.Category, entityAttribute.EditorHelper,
					entityAttribute.Icon, entityAttribute.Flags);
		}

        [Serializable]
        public class EntityException : Exception
        {
            public EntityException()
            {
            }

            public EntityException(string message)
                : base(message)
            {
            }

            public EntityException(string message, Exception inner)
                : base(message, inner)
            {
            }

            protected EntityException(
                SerializationInfo info,
                StreamingContext context)
                : base(info, context)
            {
            }
        }
        #endregion
	}

	public enum EntitySlotFlags
	{
		Render = 0x0001,  // Draw this slot.
		RenderNearest = 0x0002,  // Draw this slot as nearest.
		RenderWithCustomCamera = 0x0004,  // Draw this slot using custom camera passed as a Public ShaderParameter to the entity.
		IgnorePhysics = 0x0010,  // This slot will ignore physics events sent to it.
		BreakAsEntity = 0x020,
		RenderInCameraSpace = 0x0040, // This slot position is in camera space 
		RenderAfterPostProcessing = 0x0080, // This slot position is in camera space 
		BreakAsEntityMP = 0x0100, // In MP this an entity that shouldn't fade or participate in network breakage
	}
}
