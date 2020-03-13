using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.Controllers.Helpers {
	public class LogHelper {
		public static bool LogAction(Models.Log log) {
			if (log.Description.Length == 0)
				log.Description = "[NO DESCRIPTION GIVEN]";
			if (log.UserId <= 0)
				log.Description = "[INVALID USER] " + log.Description;
			if (log.ModifiedId <= 0)
				log.Description = "[INVALID MODIFIED ID] " + log.Description;
			if (log.Changes.Length == 0 && (log.Action == Models.Log.ActionType.ModifyInstaller || log.Action == Models.Log.ActionType.ModifyManager
					|| log.Action == Models.Log.ActionType.ModifySite || log.Action == Models.Log.ActionType.ModifyUser)) {
				log.Description = "[NO CHANGES GIVEN] " + log.Description;
				log.Changes = "[NO CHANGES GIVEN]";
			}

			using (var context = new Data.ApplicationDbContext()) {
				context.Add(log);
				context.SaveChanges();
			}
			return true;
		}

		public static bool LogAction(Models.Log.ActionType action, int userId, int modifiedId, String desc, String changes) {
			return LogAction(new Models.Log(userId, action, desc, DateTime.UtcNow, modifiedId, changes));
		}

		public static bool LogAction(Models.Log.ActionType action, int userId, int modifiedId, String desc) {
			return LogAction(action, userId, modifiedId, desc, "");
		}

		public static bool NotifyAction(Models.Log.ActionType action, Object data) {
			String configValue = "";
			if (action == Models.Log.ActionType.ActivateSite)
				configValue = "Callbacks:NewActivation";
			else if (action == Models.Log.ActionType.ApproveSite)
				configValue = "Callbacks:ActivationApproved";
			else if (action == Models.Log.ActionType.CreateAdjustment)
				configValue = "Callbacks:AdjustmentRequested";
			else
				return false;

			try {
				var client = new HttpClient();
				client.PostAsync(Startup.Configuration.GetValue<string>(configValue), new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
			} catch (Exception e) {
				return false;
			}

			return true;
		}
	}
}
