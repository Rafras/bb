namespace BoomBit.HyperCasual
{
   using Coredian;

   using static Coredian.Diagnostics.Performance;

   using UnityEngine;

#if HCSDK_DIAGNOSTICS
   /// <summary>
   /// Class responsible for diagnostics of the start time of displaying advertisements.
   /// </summary>
   public static class TimeAverage
   {
      /// <summary>
      /// Gets time average.
      /// </summary>
      /// <returns>Average value</returns>
      private static float GetAverageTime()
      {
         var timeAverageValue = PlayerPrefs.GetFloat("timeAverage", 0);
         Log.Info("Getting average time..." + timeAverageValue);

         return timeAverageValue;
      }

      /// <summary>
      /// Cleans time average value.
      /// </summary>
      public static void ResetAverageTime()
      {
         Log.Info("Resets average time.");
         PlayerPrefs.SetFloat("timeAverage", 0f);
         PlayerPrefs.SetFloat("n", 0f);
         PlayerPrefs.Save();
      }

      /// <summary>
      /// Calculates average time.
      /// </summary>
      /// <returns>Value of time average.</returns>
      public static float CalculateAverage()
      {
         Log.Info("Calculates average.");

         var iterator = PlayerPrefs.GetFloat("n", 0);
         var newIterator = iterator + 1;
         var oldAverage = GetAverageTime();

         var text = TryGetExecutionTime("counting_time", out Value).ToString();
         Log.Info("Dictionary value " + text);
         if (TryGetExecutionTime("counting_time", out Value))
         {
            var currentAverage = (oldAverage + Value) / newIterator;
            PlayerPrefs.SetFloat("n", newIterator);
            PlayerPrefs.SetFloat("timeAverage", currentAverage);
            PlayerPrefs.Save();

            Log.InfoFormat("Average calculated with TryExecutionTime method.");
            Log.InfoFormat("Average time: the current average is => " + currentAverage);

            GetAverageTime();

            return currentAverage;
         }

         const float minusValue = -1f;
         return minusValue;
      }
   }

#endif
}