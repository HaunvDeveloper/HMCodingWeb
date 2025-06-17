using HMCodingWeb.Models;

namespace HMCodingWeb.Services
{
    public class UserListService
    {
        private readonly OnlineCodingWebContext _context;
        public UserListService(OnlineCodingWebContext context)
        {
            _context = context;
        }

        public string GetColumnName(int columnIndex)
        {
            // Map DataTable column index to model property
            switch (columnIndex)
            {
                case 1: return "username";
                case 2: return "fullname";
                case 3: return "programlanguage";
                case 4: return "point";
                case 5: return "rank";
                default: return "rank"; // Default to rank
            }
        }

        public IOrderedQueryable<User> ApplyOrder(IQueryable<User> query, string columnName, string direction)
        {
            switch (columnName.ToLower())
            {
                case "username":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Username)
                        : query.OrderByDescending(u => u.Username);
                case "fullname":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Fullname ?? "")
                        : query.OrderByDescending(u => u.Fullname ?? "");
                case "programlanguage":
                    return direction == "asc"
                        ? query.OrderBy(u => u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "")
                        : query.OrderByDescending(u => u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "");
                case "point":
                    return direction == "asc"
                        ? query.OrderBy(u => u.Point)
                        : query.OrderByDescending(u => u.Point);
                case "rank":
                    return direction == "asc"
                        ? query.OrderBy(u => u.RankId ?? int.MinValue)
                        : query.OrderByDescending(u => u.RankId ?? int.MinValue);
                default:
                    return query.OrderByDescending(u => u.RankId ?? int.MinValue)
                                .ThenByDescending(u => u.Point);
            }
        }

        public IOrderedQueryable<User> ApplyThenOrder(IOrderedQueryable<User> query, string columnName, string direction)
        {
            switch (columnName.ToLower())
            {
                case "username":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Username)
                        : query.ThenByDescending(u => u.Username);
                case "fullname":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Fullname ?? "")
                        : query.ThenByDescending(u => u.Fullname ?? "");
                case "programlanguage":
                    return direction == "asc"
                        ? query.ThenBy(u => u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "")
                        : query.ThenByDescending(u => u.ProgramLanguage != null ? u.ProgramLanguage.ProgramLanguageName : "");
                case "point":
                    return direction == "asc"
                        ? query.ThenBy(u => u.Point)
                        : query.ThenByDescending(u => u.Point);
                case "rank":
                    return direction == "asc"
                        ? query.ThenBy(u => u.RankId ?? int.MaxValue)
                        : query.ThenByDescending(u => u.RankId ?? int.MaxValue);
                default:
                    return query.ThenByDescending(u => u.RankId ?? int.MaxValue)
                                .ThenByDescending(u => u.Point);
            }
        }


    }
}
