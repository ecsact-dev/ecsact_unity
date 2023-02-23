using System.Collections.Generic;
using System;

namespace Ecsact {

namespace Details {
class ExecutionEntityCallbacks {
	public Int32 AddCallback(EcsactRuntime.EntityIdCallback callback) {
		callbacks.Add(entity_id_counter, callback);
		return entity_id_counter++;
	}

	public bool GetAndClearCallback(
		Int32 placeholderId,
		out EcsactRuntime.EntityIdCallback callback
	) {
		var hasCallback = callbacks.TryGetValue(placeholderId, out callback);

		if(hasCallback) {
			callbacks.Remove(placeholderId);
			return true;
		}

		return false;
	}

	private Int32 entity_id_counter = 0;

	private Dictionary<Int32, EcsactRuntime.EntityIdCallback> callbacks = new();
};
}

}
