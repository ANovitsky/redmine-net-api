﻿/*
   Copyright 2011 - 2015 Adrian Popescu, Dorin Huzum.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using Redmine.Net.Api.JSonConverters;
using Redmine.Net.Api.Types;
using Version = Redmine.Net.Api.Types.Version;

namespace Redmine.Net.Api
{
    public static partial class RedmineSerialization
    {
        private static readonly Dictionary<Type, JavaScriptConverter> converters = new Dictionary<Type, JavaScriptConverter>
        {
            {typeof (Issue), new IssueConverter()},
            {typeof (Project), new ProjectConverter()},
            {typeof (User), new UserConverter()},
            {typeof (UserGroup), new UserGroupConverter()},
            {typeof (News), new NewsConverter()},
            {typeof (Query), new QueryConverter()},
            {typeof (Version), new VersionConverter()},
            {typeof (Attachment), new AttachmentConverter()},
            {typeof (IssueRelation), new IssueRelationConverter()},
            {typeof (TimeEntry), new TimeEntryConverter()},
            {typeof (IssueStatus),new IssueStatusConverter()},
            {typeof (Tracker),new TrackerConverter()},
            {typeof (TrackerCustomField),new TrackerCustomFieldConverter()},
            {typeof (IssueCategory), new IssueCategoryConverter()},
            {typeof (Role), new RoleConverter()},
            {typeof (ProjectMembership), new ProjectMembershipConverter()},
            {typeof (Group), new GroupConverter()},
            {typeof (GroupUser), new GroupUserConverter()},
            {typeof (Error), new ErrorConverter()},
            {typeof (IssueCustomField), new IssueCustomFieldConverter()},
            {typeof (ProjectTracker), new ProjectTrackerConverter()},
            {typeof (Journal), new JournalConverter()},
            {typeof (TimeEntryActivity), new TimeEntryActivityConverter()},
            {typeof (IssuePriority), new IssuePriorityConverter()},
            {typeof (WikiPage), new WikiPageConverter()},
            {typeof (Detail), new DetailConverter()},
            {typeof (ChangeSet), new ChangeSetConverter()},
            {typeof (Membership), new MembershipConverter()},
            {typeof (MembershipRole), new MembershipRoleConverter()},
            {typeof (IdentifiableName), new IdentifiableNameConverter()},
            {typeof (Permission), new PermissionConverter()},
            {typeof (IssueChild), new IssueChildConverter()},
            {typeof (ProjectIssueCategory), new ProjectIssueCategoryConverter()},
            {typeof (Watcher), new WatcherConverter()},
            {typeof (Upload), new UploadConverter()},
            {typeof (ProjectEnabledModule), new ProjectEnabledModuleConverter()},
            {typeof (CustomField), new CustomFieldConverter()},
            {typeof (CustomFieldRole), new CustomFieldRoleConverter()},
            {typeof (CustomFieldPossibleValue), new CustomFieldPossibleValueConverter()}
        };

        public static Dictionary<Type, JavaScriptConverter> Converters { get { return converters; } }

        static int _maxJsonLength = int.MaxValue;

        public static int MaxJsonLength
        {
            get
            {
                return _maxJsonLength;
                
            }
            set { _maxJsonLength = value; }
        } 

        public static string JsonSerializer<T>(T type) where T : new()
        {
            try
            {
                var ser = new JavaScriptSerializer()
                {
                    MaxJsonLength = MaxJsonLength
                };
                ser.RegisterConverters(new[] { converters[typeof(T)] });
                var jsonString = ser.Serialize(type);
                return jsonString;
            }
            catch (Exception)
            {
                throw;
                //return null;
            }
        }

        /// <summary>
        /// JSON Deserialization
        /// </summary>
        public static List<T> JsonDeserializeToList<T>(string jsonString, string root) where T : class, new()
        {
            int totalCount;
            return JsonDeserializeToList<T>(jsonString, root, out totalCount);
        }

        public static object JsonDeserializeToList(string jsonString, string root, Type type, out int totalCount)
        {
            totalCount = 0;
            if (String.IsNullOrEmpty(jsonString)) return null;

            var ser = new JavaScriptSerializer()
            {
                MaxJsonLength = MaxJsonLength
            };
            ser.RegisterConverters(new[] { converters[type] });
            var dic = ser.Deserialize<Dictionary<string, object>>(jsonString);
            if (dic == null) return null;

            object obj, tc;

            if (dic.TryGetValue("total_count", out tc)) totalCount = (int)tc;

            if (dic.TryGetValue(root.ToLower(CultureInfo.InvariantCulture), out obj))
            {
                var list = new ArrayList();
                if (type == typeof(Error))
                {
                    string info = null;
                    foreach (var item in (ArrayList)obj)
                    {
                        var arrayList = item as ArrayList;
                        if (arrayList != null)
                        {
                            foreach (var item2 in arrayList) info += item2 as string + " ";
                        }
                        else
                            info += item as string + " ";
                    }
                    var err = new Error { Info = info };
                    list.Add(err);
                }
                else
                {
                    AddToList(ser, list, type, obj);
                }
                return list;
            }
            return null;
        }

        /// <summary>
        /// JSON Deserialization
        /// </summary>
        public static List<T> JsonDeserializeToList<T>(string jsonString, string root, out int totalCount) where T : class,new()
        {
            try
            {
                var result = JsonDeserializeToList(jsonString, root, typeof(T), out totalCount);

                return result == null ? null : ((ArrayList)result).OfType<T>().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("An error on JsonDeserializeToList<" + typeof(T).Name + "> from '" + jsonString.Substring(0, jsonString.Length > 800 ? 799 : jsonString.Length) + "'", ex);
            }
        }

        private static void AddToList(JavaScriptSerializer ser, IList list, Type type, object obj)
        {
            foreach (var item in (ArrayList)obj)
            {
                if (item is ArrayList)
                {
                    AddToList(ser, list, type, item);
                }
                else
                {
                    var o = ser.ConvertToType(item, type);
                    list.Add(o);
                }
            }
        }

        public static T JsonDeserialize<T>(string jsonString, string root) where T : new()
        {
            var type = typeof(T);
            try
            {
                var result = JsonDeserialize(jsonString, type, root);
                if (result == null) throw new NullReferenceException("JsonDeserialize return null");

                return (T)result;
            }
            catch (Exception ex)
            {
                throw new Exception("An error on JsonDeserialize<" + type.Name + "> from '" + jsonString + "'", ex);
            }
        }

        public static object JsonDeserialize(string jsonString, Type type, string root)
        {
            if (String.IsNullOrEmpty(jsonString)) return null;

            var ser = new JavaScriptSerializer()
            {
                MaxJsonLength = MaxJsonLength
            };
            ser.RegisterConverters(new[] { converters[type] });

            var dic = ser.Deserialize<Dictionary<string, object>>(jsonString);
            if (dic == null) return null;

            object obj;
            if (dic.TryGetValue(root ?? type.Name.ToLower(CultureInfo.InvariantCulture), out obj))
            {
                var deserializedObject = ser.ConvertToType(obj, type);

                return deserializedObject;
            }
            return null;
        }
    }
}