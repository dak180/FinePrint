﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSP;
using KSPAchievements;
using FinePrint.Contracts.Parameters;

namespace FinePrint.Contracts
{
	public class RoverContract : Contract
	{
		CelestialBody targetBody = null;
		double centerLatitude = 0.0;
		double centerLongitude = 0.0;

		protected override bool Generate()
		{
            if (AreWheelsUnlocked() == false)
                return false;

            //Allow four contracts in pocket but only two on the board at a time.
            int offeredContracts = 0;
            int activeContracts = 0;
            foreach (AerialContract contract in ContractSystem.Instance.GetCurrentContracts<AerialContract>())
            {
                if (contract.ContractState == Contract.State.Offered)
                    offeredContracts++;
                else if (contract.ContractState == Contract.State.Active)
                    activeContracts++;
            }

            if (offeredContracts >= 2 || activeContracts >= 4)
                return false;

			System.Random generator = new System.Random(this.MissionSeed);
			double range = 10000.0;
			List<CelestialBody> bodies = GetBodies_Reached(true, false);

			if (bodies.Count == 0)
				return false;

			targetBody = bodies[generator.Next(0, bodies.Count)];

			if (targetBody.GetName() == "Jool")
			{
				targetBody = Util.RandomJoolianMoon();

				if (targetBody.GetName() == "Jool" || targetBody == null)
					return false;
			}

			int waypointCount = 0;

			if (this.prestige == Contract.ContractPrestige.Trivial)
			{
				waypointCount = 3;
				range = 1000 + 1000 * targetBody.GeeASL;
			}
			else if (this.prestige == Contract.ContractPrestige.Significant)
			{
				waypointCount = 5;
				range = 2000 + 2000 * targetBody.GeeASL;
			}
			else if (this.prestige == Contract.ContractPrestige.Exceptional)
			{
				waypointCount = 7;
				range = 3000 + 3000 * targetBody.GeeASL;
			}

			WaypointManager.ChooseRandomPosition(out centerLatitude, out centerLongitude, targetBody.GetName(), false);
			int secret = UnityEngine.Random.Range(0, waypointCount);

			for (int x = 0; x < waypointCount; x++)
			{
				ContractParameter newParameter;

				if (x == secret)
					newParameter = this.AddParameter(new RoverWaypointParameter(x, targetBody, centerLatitude, centerLongitude, range, true), null);
				else
					newParameter = this.AddParameter(new RoverWaypointParameter(x, targetBody, centerLatitude, centerLongitude, range, false), null);

				newParameter.SetFunds(5000.0f, targetBody);
				newParameter.SetReputation(10.0f, targetBody);
                newParameter.SetScience(10.0f, targetBody);
			}

			base.AddKeywords(new string[] { "roversearch" });
			base.SetExpiry();
			base.SetDeadlineYears(5.0f, targetBody);
			base.SetFunds(4000.0f, 17500.0f, targetBody);
			base.SetReputation(50.0f, 25.0f, targetBody);
			return true;
		}

		public override bool CanBeCancelled()
		{
			return true;
		}

		public override bool CanBeDeclined()
		{
			return true;
		}

		protected override string GetHashString()
		{
			return (this.MissionSeed.ToString() + this.DateAccepted.ToString());
		}

		protected override string GetTitle()
		{
			return "Chart source of data anomaly near " + Util.generateSiteName(MissionSeed, (targetBody == Planetarium.fetch.Home)) + " on " + targetBody.GetName() + " with a rover.";
		}

		protected override string GetDescription()
		{
			//those 3 strings appear to do nothing
			return TextGen.GenerateBackStories(Agent.Name, Agent.GetMindsetString(), "driving a rover", "joyriding", "surface travel", new System.Random().Next());
		}

		protected override string GetSynopsys()
		{
			return "We've noticed an interesting anomaly in the general area of " + Util.generateSiteName(MissionSeed, (targetBody == Planetarium.fetch.Home)) + " on " + targetBody.GetName() + ". It's a wide area, so you will need a rover to find it.";
		}

		protected override string MessageCompleted()
		{
			return "You have successfully found the source of the data anomaly in " + Util.generateSiteName(MissionSeed, (targetBody == Planetarium.fetch.Home)) + " on " + targetBody.GetName() + ".";
		}

		protected override void OnLoad(ConfigNode node)
		{
			Util.LoadNode(node, "RoverContract", "targetBody", ref targetBody, Planetarium.fetch.Home);
			Util.LoadNode(node, "RoverContract", "centerLatitude", ref centerLatitude, 0.0);
			Util.LoadNode(node, "RoverContract", "centerLongitude", ref centerLongitude, 0.0);
		}

		protected override void OnSave(ConfigNode node)
		{
			int bodyID = targetBody.flightGlobalsIndex;
			node.AddValue("targetBody", bodyID);
			node.AddValue("centerLatitude", centerLatitude);
			node.AddValue("centerLongitude", centerLongitude);
		}

		//for testing purposes
		public override bool MeetRequirements()
		{
            return true;
		}

		protected static bool AreWheelsUnlocked()
		{
			if (Util.haveTechnology("roverWheel1"))
				return true;
			if (Util.haveTechnology("roverWheel2"))
				return true;
			if (Util.haveTechnology("roverWheel3"))
				return true;
			if (Util.haveTechnology("wheelMed"))
				return true;

			return false;
		}

		protected static bool AreProbesUnlocked()
		{
			if (Util.haveTechnology("probeCoreCube"))
				return true;
			if (Util.haveTechnology("probeCoreHex"))
				return true;
			if (Util.haveTechnology("probeCoreOcto"))
				return true;
			if (Util.haveTechnology("probeCoreOcto2"))
				return true;
			if (Util.haveTechnology("probeCoreSphere"))
				return true;
			if (Util.haveTechnology("probeStackLarge"))
				return true;
			if (Util.haveTechnology("probeStackSmall"))
				return true;

			return false;
		}
	}
}