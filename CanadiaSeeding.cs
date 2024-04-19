// example file used for defining the search
// includes things like specific day search of the cart
// and specific location forage spawns on a planned run day

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SeedFinding
{
    public class CanadiaSeeding
    {

        const double MIN_LUCK = 0.09999;
        static readonly List<string> FRIENDS = new List<string> { "Lewis", "Robin" };

        static (bool, int) ValidGeode(int seed, long multiPlayerId, int geodesCracked)
        {

            (string id, int quantity) = Mines.GetGeodeContents1_6_4(seed, multiPlayerId, geodesCracked, Geode.Geode);

            if (id == "(O)378" && quantity >= 20)
            {
                return (true, quantity);

            }
            else
            {
                return (false, quantity);
            }

        }

        static (bool, HashSet<Trash1_6.Trash.TrashItem>) ValidTrash(int seed)
        {

            // foreach (var item in Trash1_6.Trash.getAllTrash(seed, 1, 0.1))
            // {
            //     Console.WriteLine(String.Format("{0}", item.Id));
            // }
            var items = Trash1_6.Trash.getAllTrash(seed, 1, MIN_LUCK);
            var geode_items = new HashSet<Trash1_6.Trash.TrashItem>(items.Where(x => x.Id == "535"));
            var dish_of_day_items = new HashSet<Trash1_6.Trash.TrashItem>(items.Where(x => x.Id == "DishOfTheDay"));
            bool valid = geode_items.Count > 0 && dish_of_day_items.Count() > 0;
            return (valid, items);
        }

        static (bool, List<int>) ValidWeather(int gameID)
        {
            var rain_days = Enumerable.Range(1, 12).Where(day =>
                Weather.getWeather(day, gameID) == Weather.WeatherType.Rain || Weather.getWeather(day, gameID) == Weather.WeatherType.Storm
            ).ToList();

            return (rain_days.Count > 3, rain_days);
        }

        static (bool, NightEvents1_6.NightEvent.Event) ValidFairy(int gameID)
        {
            var nightEvent = NightEvents1_6.NightEvent.GetEvent(gameID, 2);

            return (nightEvent == NightEvents1_6.NightEvent.Event.Fairy, nightEvent);
        }

        static (bool, double) ValidLuck(StepPredictions.StepResult prediction)
        {
            return (prediction.DailyLuck > MIN_LUCK, prediction.DailyLuck);
        }

        static (bool, int) ValidDishOfTheDay(StepPredictions.StepResult prediction, int dishId)
        {
            return (prediction.Dish == dishId, prediction.DishAmount);
        }

        static bool ValidDay0Seed(int gameId, long _multiPlayerId, bool curate)
        {
            var prediction = StepPredictions.Predict(gameId, 0 /* day */, 0 /* steps */, FRIENDS, 0);

            (bool luck_valid, double luck) = ValidLuck(prediction);
            if (!luck_valid) return false;

            (bool dish_valid, int dish_quantity) = ValidDishOfTheDay(prediction, 213);
            if (!dish_valid) return false;

            if (curate)
            {
                Console.WriteLine(gameId);
                Console.WriteLine(string.Format("  luck: {0}", luck));
                Console.WriteLine(string.Format("  dish quantity: {0}", dish_quantity));
            }

            return false;
        }

        static bool ValidDay1Seed(int gameId, long multiPlayerId, bool curate)
        {
            (bool geode_valid, int quantity) = ValidGeode(gameId, multiPlayerId, 1);
            if (!geode_valid) return false;

            (bool trash_valid, var trash_items) = ValidTrash(gameId);
            if (!trash_valid) return false;

            (bool weather_valid, var rain_days) = ValidWeather(gameId);
            if (!weather_valid) return false;

            (bool fairy_valid, var night_event) = ValidFairy(gameId);
            if (!fairy_valid) return false;

            if (curate)
            {
                Console.WriteLine(gameId);
                Console.WriteLine(string.Format("  copper from geode: {0}", quantity));
                Console.WriteLine(string.Format("  trash:"));
                foreach (var item in trash_items) {
                Console.WriteLine(string.Format("  * {0}", item));
                }

                Console.WriteLine(string.Format("  rain days: {0}", String.Join(",", rain_days)));
                Console.WriteLine(string.Format("  night event: {0}\n", night_event));
            }
            return true;
        }


        public static double Day0Search(long multiPlayerId, int numSeeds, int blockSize, out List<int> validSeeds, bool curate)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var bag = new ConcurrentBag<int>();
            var partioner = Partitioner.Create(0, numSeeds, blockSize);
            Parallel.ForEach(partioner, (range, loopState) =>
            {
                for (int seed = range.Item1; seed < range.Item2; seed++)
                {
                    if (ValidDay0Seed(seed, multiPlayerId, curate))
                    {
                        bag.Add(seed);
                        if (!curate)
                        {
                            Console.WriteLine(seed);
                        }

                    }
                }
            });
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Found: {bag.Count} sols in {seconds.ToString("F2")} s");
            validSeeds = bag.ToList();
            validSeeds.Sort();
            return seconds;
        }

        public static double Day1Search(long multiPlayerId, int numSeeds, int blockSize, out List<int> validSeeds, bool curate)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var bag = new ConcurrentBag<int>();
            var partioner = Partitioner.Create(0, numSeeds, blockSize);
            Parallel.ForEach(partioner, (range, loopState) =>
            {
                for (int seed = range.Item1; seed < range.Item2; seed++)
                {
                    if (ValidDay1Seed(seed, multiPlayerId, curate))
                    {
                        bag.Add(seed);
                        if (!curate)
                        {
                            Console.WriteLine(seed);
                        }

                    }
                }
            });
            double seconds = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Found: {bag.Count} sols in {seconds.ToString("F2")} s");
            validSeeds = bag.ToList();
            validSeeds.Sort();
            return seconds;
        }

    }
}

