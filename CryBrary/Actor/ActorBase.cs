﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

using CryEngine.Native;

namespace CryEngine
{
	/// <summary>
	/// Base class which all actors must derive from. Includes basic callbacks.
	/// </summary>
	public abstract class ActorBase : EntityBase
	{
		/// <summary>
		/// Initializes the player.
		/// </summary>
		/// <param name="actorInfo"></param>
		internal virtual void InternalSpawn(ActorInfo actorInfo)
		{
			System.Diagnostics.Contracts.Contract.Requires(actorInfo.ChannelId > 0);
			Id = new EntityId(actorInfo.Id);
			this.SetActorHandle(new HandleRef(this, actorInfo.ActorPtr));
			this.SetEntityHandle(new HandleRef(this, actorInfo.EntityPtr));

			ChannelId = actorInfo.ChannelId;

			// actor must have physics
			Physics.Type = PhysicalizationType.Rigid;

			OnSpawn();
		}

		#region Callbacks
		/// <summary>
		/// Called after successful actor creation via Actor.Create.
		/// </summary>
		public virtual void OnSpawn() { }
		#endregion

		#region Overrides
		/// <summary>
		/// Removes this actor from the world.
		/// </summary>
		/// <param name="forceRemoveNow"></param>
		public override void Remove(bool forceRemoveNow = false)
		{
			if (forceRemoveNow)
				throw new NotSupportedException("forceRemoveNow is not supported for actor types.");

			Actor.Remove(Id);
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;

				hash = hash * 29 + ScriptId.GetHashCode();
				hash = hash * 29 + Id.GetHashCode();
				hash = hash * 29 + ChannelId.GetHashCode();
				hash = hash * 29 + this.GetActorHandle().Handle.GetHashCode();
				hash = hash * 29 + this.GetEntityHandle().Handle.GetHashCode();

				return hash;
			}
		}

		internal override void OnScriptReloadInternal()
		{
			this.SetActorHandle(new HandleRef(this, NativeMethods.Actor.GetActorInfoById(Id).ActorPtr));

			base.OnScriptReloadInternal();
		}
		#endregion

		/// <summary>
		/// Checks if this actor is controlled by the local client. See <see cref="Actor.LocalClient"/>.
		/// </summary>
		public bool IsLocalClient { get { return Actor.LocalClient == this; } }

		/// <summary>
		/// Sets / gets the current health of this actor.
		/// </summary>
		public virtual float Health { get { return NativeMethods.Actor.GetPlayerHealth(this.GetActorHandle().Handle); } set { NativeMethods.Actor.SetPlayerHealth(this.GetActorHandle().Handle, value); } }
		/// <summary>
		/// Sets / gets the max health value for this actor.
		/// </summary>
		public virtual float MaxHealth { get { return NativeMethods.Actor.GetPlayerMaxHealth(this.GetActorHandle().Handle); } set { NativeMethods.Actor.SetPlayerMaxHealth(this.GetActorHandle().Handle, value); } }

		/// <summary>
		/// Determines if this actor had died. Returns true if <see cref="Health"/> is equal to or below 0.
		/// </summary>
		public bool IsDead { get { return Health <= 0; } }

		internal HandleRef ActorHandleRef { get; set; }
		/// <summary>
		/// The channel id, index to the net channel in use by this actor.
		/// </summary>
		public int ChannelId { get; set; }
	}
}