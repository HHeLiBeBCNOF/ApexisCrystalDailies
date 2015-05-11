
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Xml.Linq;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using System.Diagnostics;


namespace Honorbuddy.Quest_Behaviors.HBRelogSkip 
{
    [ServiceContract]
    interface IRemotingApi
    {
        [OperationContract]
        bool Init(int hbProcID);
        [OperationContract (IsOneWay=true)]
        void RestartHB(int hbProcID);
        [OperationContract(IsOneWay = true)]
        void RestartWow(int hbProcID);
        [OperationContract]
        string[] GetProfileNames();
        [OperationContract]
        string GetCurrentProfileName(int hbProcID);
        [OperationContract(IsOneWay = true)]
        void StartProfile(string profileName);
        [OperationContract(IsOneWay = true)]
        void StopProfile(string profileName);
        [OperationContract(IsOneWay = true)]
        void PauseProfile(string profileName);
        [OperationContract(IsOneWay = true)]
        void IdleProfile(string profileName, TimeSpan time);
        [OperationContract(IsOneWay = true)]
        void Logon(int hbProcID, string character, string server, string customClass, string botBase, string profilePath);
        [OperationContract]
        int GetProfileStatus(string profileName);
        [OperationContract(IsOneWay = true)]
        void SetProfileStatusText(int hbProcID, string status);
        [OperationContract(IsOneWay = true)]
        void SetBotInfoToolTip(int hbProcID, string tooltip);
        [OperationContract(IsOneWay = true)]
        void SkipCurrentTask(string profileName);
    }

	[CustomBehaviorFileName(@"HBRelogSkip")]
	public class HBRelogSkip : QuestBehaviorBase
	{
		public HBRelogSkip(Dictionary<string, string> args)
			: base(args)
		{
			QBCLog.BehaviorLoggingContext = this;
		}

		protected override void EvaluateUsage_DeprecatedAttributes(XElement xElement)
		{
			//// EXAMPLE: 
			//UsageCheck_DeprecatedAttribute(xElement,
			//    Args.Keys.Contains("Nav"),
			//    "Nav",
			//    context => string.Format("Automatically converted Nav=\"{0}\" attribute into MovementBy=\"{1}\"."
			//                              + "  Please update profile to use MovementBy, instead.",
			//                              Args["Nav"], MovementBy));
		}
		protected override void EvaluateUsage_SemanticCoherency(XElement xElement)
		{
			//// EXAMPLE:
			//UsageCheck_SemanticCoherency(xElement,
			//    (!MobIds.Any() && !FactionIds.Any()),
			//    context => "You must specify one or more MobIdN, one or more FactionIdN, or both.");
			//
			//const double rangeEpsilon = 3.0;
			//UsageCheck_SemanticCoherency(xElement,
			//    ((RangeMax - RangeMin) < rangeEpsilon),
			//    context => string.Format("Range({0}) must be at least {1} greater than MinRange({2}).",
			//                  RangeMax, rangeEpsilon, RangeMin)); 
		}



		protected override Composite CreateMainBehavior()
		{
			return new ActionRunCoroutine(ctx => MainCoroutine());
		}

        static public bool IsConnected { get; private set; }
        static internal IRemotingApi HBRelogRemoteApi { get; private set; }
        static internal int HbProcId { get; private set; }
        static internal string CurrentProfileName { get; private set; }
        static ChannelFactory<IRemotingApi> _pipeFactory;

		public override void OnStart()
		{
			// Let QuestBehaviorBase do basic initializaion of the behavior, deal with bad or deprecated attributes,
			// capture configuration state, install BT hooks, etc.  This will also update the goal text.
			var isBehaviorShouldRun = OnStart_QuestBehaviorCore();

			// If the quest is complete, this behavior is already done...
			// So we don't want to falsely inform the user of things that will be skipped.
			if (isBehaviorShouldRun)
			{
                HbProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
                _pipeFactory = new ChannelFactory<IRemotingApi>(new NetNamedPipeBinding(),
                        new EndpointAddress("net.pipe://localhost/HBRelog/Server"));
			    try
			    {

			        HBRelogRemoteApi = _pipeFactory.CreateChannel();

			        IsConnected = HBRelogRemoteApi.Init(HbProcId);
			        if (IsConnected)
			        {
			            QBCLog.DeveloperInfo("Connected to HBRelog Server");
			            CurrentProfileName = HBRelogRemoteApi.GetCurrentProfileName(HbProcId);
			            QBCLog.DeveloperInfo(string.Format("HBRelog Current Profile: {0}", CurrentProfileName));
			        }
			        else
			        {
			            QBCLog.Error("Could Not Connect to HBRelog Server at net.pipe://localhost/HBRelog/Server");
			        }
			    }
			    catch (Exception ex)
			    {
			        QBCLog.Error(
			            "Could not make endpoint connection to HBRelog Application.  You may not be running under HBRelog. Ignoring");
			        CurrentProfileName = string.Empty;
			    }
			}
		}
		private async Task<bool> MainCoroutine()
		{
            if (!string.IsNullOrEmpty(CurrentProfileName))
		    {
		        QBCLog.Info("Skipping Current HBRelogTask... Bye Bye Now");
                HBRelogRemoteApi.SkipCurrentTask(CurrentProfileName);
		        BehaviorDone();
		        return true;
		    }
		    else
		    {
		        QBCLog.Error("Could not connect to HBRelog Server or Invalid Profile. Could not Skip Task");
		        BehaviorDone();
		        return true;
		    }
		}
	}
}
