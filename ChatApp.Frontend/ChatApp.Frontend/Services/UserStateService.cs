using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Frontend.Services
{
    public class UserStateService
    {
        public string token {  get; set; }
        public Guid UserId {  get; set; }
        public string Username {  get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(token);
    }
}
