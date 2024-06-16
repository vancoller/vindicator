using Vindicator.Service.Enums;

namespace cAlgo.API
{
    public static class LocalStorageExtensions
    {
        private static LocalStorageScope scope = LocalStorageScope.Device;

        //Get the last stored index
        public static int GetLastStoredIndex(this LocalStorage localStorage)
        {
            var key = LocalStorageKeys.LastStoredIndex.ToString();
            var index = localStorage.GetObject<int>($"{key}", scope);
            return index;
        }

        //Get next index
        public static int GetNextIndex(this LocalStorage localStorage)
        {
            var index = localStorage.GetLastStoredIndex();
            return index + 1;
        }

        //Set the last stored index
        public static void SetLastStoredIndex(this LocalStorage localStorage, int index)
        {
            var key = LocalStorageKeys.LastStoredIndex.ToString();
            localStorage.SetObject($"{key}", index, scope);
        }

        public static int GetStoredPosition(this LocalStorage localStorage, int index)
        {
            var key = LocalStorageKeys.Position.ToString();
            var positionId = localStorage.GetObject<int>($"{key}{index}", scope);
            return positionId;
        }

        public static void StorePosition(this LocalStorage localStorage, int index, int positionId)
        {
            var key = LocalStorageKeys.Position.ToString();
            localStorage.SetObject($"{key}{index}", positionId, scope);
        }

        public static void RemoveStoredPosition(this LocalStorage localStorage, int index)
        {
            var key = LocalStorageKeys.Position.ToString();
            localStorage.Remove($"{key}{index}", scope);
        }

        //public static int GetAndSetReportIndex(this LocalStorage localStorage)
        //{
        //    var key = LocalStorageKeys.BacktestReport.ToString();

        //    //Get
        //    localStorage.Reload(scope);
        //    var index = localStorage.GetObject<int>(key, LocalStorageScope.Type);

        //    //Set
        //    index++;
        //    localStorage.SetObject(key, index, scope);
        //    localStorage.Flush(scope);

        //    //Return
        //    return index;
        //}
    }
}
