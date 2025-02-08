using System;
using MudBlazor;
namespace Siren.Components.Theme
{
    public static class SirenIcons
    {
        public static string Rounded => "material-symbols-rounded/";
        
        public static string Round(string icon) => Rounded + icon;
        public static string Regular => "fa-regular";

        public static string Light => "fa-light";

        public static string Code => Round("code");

        public static string Collections => Round("dashboard_customize");

        public static string Collection => Round("grid_view");

        public static string AddToCollection => Round("new_window");

        public static string AddRequestToCollection => Round("add_box");

        public static string AddCollection => Round("library_add");

        public static string CollectionItemAdded => Round("check_box");

        public static string History => Round("history");

        public static string DeleteHistory => Round("delete_history");

        public static string Variables => Round("dictionary"); 

        public static string LightMode => Round("light_mode");

        public static string DarkMode => Round("dark_mode");

        public static string Close => Round("close");

        public static string CancelEdit => Round("edit_off");

        public static string Back => Round("arrow_back");

        public static string CollapseLeft => Round("chevron_left");

        public static string ExpandRight => Round("chevron_right");

        public static string Filter => Round("filter_list");

        public static string Add => Round("add");

        public static string Refresh => Round("refresh");

        public static string Settings => Round("settings");

        public static string Gears => Round("manufacturing");

        public static string Request => Round("bigtop_updates");

        public static string Response => Round("network_ping");

        public static string Empty => Round("hide_source");

        public static string Edit => Round("edit");

        public static string LoadNewTab => Round("open_in_new");

        public static string Load => Round("output");

        public static string Delete => Round("delete");

        public static string Copy => Round("content_copy");
        
        public static string UrlImport => Round("cloud_upload");

        public static string Key => Round("key");

        public static string Value => Round("123");

        public static string Time => Round("more_time");

        public static string JustNow => Round("schedule");

        public static string AppData => Round("database");

        public static string Cookies => Round("cookie"); 

        public static string Format => Round("format_align_left");

        public static string Auth => Round("security");

        public static string Headers => Round("contract_edit");

        public static string Recent => Round("update");
    }
}

