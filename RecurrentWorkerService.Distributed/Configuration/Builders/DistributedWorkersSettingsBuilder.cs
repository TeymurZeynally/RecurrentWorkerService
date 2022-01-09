using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecurrentWorkerService.Distributed.Services.Settings;

namespace RecurrentWorkerService.Distributed.Configuration.Builders
{
	public class DistributedWorkersSettingsBuilder
	{
		private TimeSpan _heartbeatPeriod;
		private TimeSpan _heartbeatExpirationTimeout;

		internal DistributedWorkersSettingsBuilder()
		{
			_heartbeatPeriod = TimeSpan.FromSeconds(1);
			_heartbeatExpirationTimeout = TimeSpan.FromSeconds(7);
		}

		public DistributedWorkersSettingsBuilder SetHeartbeatPeriod(TimeSpan period)
		{
			_heartbeatPeriod = period;
			return this;
		}

		public DistributedWorkersSettingsBuilder SetHeartbeatExpirationTimeout(TimeSpan timeout)
		{
			_heartbeatExpirationTimeout = timeout;
			return this;
		}

		internal HeartbeatSettings Build()
		{
			return new HeartbeatSettings
			{
				HeartbeatPeriod = _heartbeatPeriod,
				HeartbeatExpirationTimeout = _heartbeatExpirationTimeout
			};
		}

	}
}
