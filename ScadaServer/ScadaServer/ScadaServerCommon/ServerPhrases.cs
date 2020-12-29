﻿/*
 * Copyright 2020 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaServerCommon
 * Summary  : The phrases used by the server and its modules
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2020
 * Modified : 2020
 */

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

namespace Scada.Server
{
    /// <summary>
    /// The phrases used by the server and its modules.
    /// <para>Фразы, используемые сервером и его модулями.</para>
    /// </summary>
    public static class ServerPhrases
    {
        // Scada.Server.Modules
        public static string LoadModuleConfigError { get; private set; }
        public static string SaveModuleConfigError { get; private set; }

        // Engine
        public static string ErrorInArchive { get; private set; }
        public static string ErrorInModule { get; private set; }

        // Archives
        public static string ArchiveMessage { get; private set; }
        public static string NullResultNotAllowed { get; private set; }
        public static string InvalidWritingPeriod { get; private set; }
        public static string WritingModeNotSupported { get; private set; }
        public static string WritingModeIsSlow { get; private set; }
        public static string DeleteOutdatedData { get; private set; }
        public static string ReadingTrendsCompleted { get; private set; }
        public static string ReadingTrendCompleted { get; private set; }
        public static string ReadingTimestampsCompleted { get; private set; }
        public static string ReadingSliceCompleted { get; private set; }
        public static string ReadingPointsCompleted { get; private set; }
        public static string WritingSliceCompleted { get; private set; }
        public static string WritingPointsCompleted { get; private set; }
        public static string QueueingPointsCompleted { get; private set; }
        public static string QueueBecameEmpty { get; private set; }
        public static string PointsWereLost { get; private set; }
        public static string UpdateCompleted { get; private set; }
        public static string ReadingEventsCompleted { get; private set; }
        public static string ReadingEventCompleted { get; private set; }
        public static string WritingEventCompleted { get; private set; }
        public static string QueueingEventCompleted { get; private set; }
        public static string EventsWereLost { get; private set; }
        public static string AckEventCompleted { get; private set; }
        public static string AckEventNotFound { get; private set; }

        // Modules
        public static string ReadDbError { get; private set; }
        public static string WriteDbError { get; private set; }

        public static void Init()
        {
            // load phrases from dictionaries, because the service user interface is multilingual
            LocaleDict dict = Locale.GetDictionary("Scada.Server.Modules");
            LoadModuleConfigError = dict.GetPhrase("LoadModuleConfigError");
            SaveModuleConfigError = dict.GetPhrase("SaveModuleConfigError");

            // set phrases depending on locale, because the service logic supports only 2 languages
            if (Locale.IsRussian)
            {
                ErrorInArchive = "Ошибка при вызове метода {0} архива {1}";
                ErrorInModule = "Ошибка при вызове метода {0} модуля {1}";

                ArchiveMessage = "Архив {0}: {1}";
                NullResultNotAllowed = "Результат метода не может быть null.";
                InvalidWritingPeriod = "Период записи должен быть положительным.";
                WritingModeNotSupported = "Режим записи не поддерживается архивом {0}.";
                WritingModeIsSlow = "Выбранный режим записи может снизить производительность";
                DeleteOutdatedData = "Удаление устаревших данных из архива {0}, которые старше {1}";
                ReadingTrendsCompleted = "Чтение трендов длины {0} успешно завершено за {1} мс";
                ReadingTrendCompleted = "Чтение тренда длины {0} успешно завершено за {1} мс";
                ReadingTimestampsCompleted = "Чтение меток времени длины {0} успешно завершено за {1} мс";
                ReadingSliceCompleted = "Чтение среза длины {0} успешно завершено за {1} мс";
                ReadingPointsCompleted = "Чтение {0} точек данных успешно завершено за {1} мс";
                WritingSliceCompleted = "Запись среза длины {0} успешно завершена за {1} мс";
                WritingPointsCompleted = "Запись {0} точек данных успешно завершена за {1} мс";
                QueueingPointsCompleted = "Постановка в очередь {0} точек данных успешно завершена за {1} мс";
                QueueBecameEmpty = "Очередь данных стала пустой";
                PointsWereLost = "{0} точек данных были потеряны";
                UpdateCompleted = "Обновление данных успешно завершено за {0} мс";
                ReadingEventsCompleted = "Чтение {0} событий успешно завершено за {1} мс";
                ReadingEventCompleted = "Чтение события успешно завершено за {0} мс";
                WritingEventCompleted = "Запись события успешно завершена за {0} мс";
                QueueingEventCompleted = "Постановка события в очередь успешно завершена за {0} мс";
                EventsWereLost = "{0} событий были потеряны";
                AckEventCompleted = "Квитирование события с ид. {0} успешно завершено за {1} мс";
                AckEventNotFound = "Квитируемое событие с ид. {0} не найдено";

                ReadDbError = "Ошибка при чтении из базы данных";
                WriteDbError = "Ошибка при записи в базу данных";
            }
            else
            {
                ErrorInArchive = "Error calling the {0} method of the {1} archive";
                ErrorInModule = "Error calling the {0} method of the {1} module";

                ArchiveMessage = "Archive {0}: {1}";
                NullResultNotAllowed = "Method result must not be null.";
                InvalidWritingPeriod = "Writing period must be positive.";
                WritingModeNotSupported = "Writing mode is not supported by the {0} archive.";
                WritingModeIsSlow = "The selected writing mode may decrease performance";
                DeleteOutdatedData = "Delete outdated data from the {0} archive older than {1}";
                ReadingTrendsCompleted = "Reading trends of length {0} completed successfully in {1} ms";
                ReadingTrendCompleted = "Reading a trend of length {0} completed successfully in {1} ms";
                ReadingTimestampsCompleted = "Reading timestamps of length {0} completed successfully in {1} ms";
                ReadingSliceCompleted = "Reading a slice of length {0} completed successfully in {1} ms";
                ReadingPointsCompleted = "Reading of {0} data points completed successfully in {1} ms";
                WritingSliceCompleted = "Writing a slice of length {0} completed successfully in {1} ms";
                WritingPointsCompleted = "Writing of {0} data points completed successfully in {1} ms";
                QueueingPointsCompleted = "Enqueueing of {0} data points completed successfully in {1} ms";
                QueueBecameEmpty = "Data queue has become empty";
                PointsWereLost = "{0} data points were lost";
                UpdateCompleted = "Data update completed successfully in {0} ms";
                ReadingEventsCompleted = "Reading of {0} events completed successfully in {1} ms";
                ReadingEventCompleted = "Reading an event completed successfully in {0} ms";
                WritingEventCompleted = "Event writing completed successfully in {0} ms";
                QueueingEventCompleted = "Enqueueing an event completed successfully in {0} ms";
                EventsWereLost = "{0} events were lost";
                AckEventCompleted = "Acknowledging an event with ID {0} completed successfully in {1} ms";
                AckEventNotFound = "Acknowledged event with ID {0} not found";

                ReadDbError = "Error reading from database";
                WriteDbError = "Error writing to database";
            }
        }
    }
}
