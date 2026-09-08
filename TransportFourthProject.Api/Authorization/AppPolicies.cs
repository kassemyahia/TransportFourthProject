namespace TransportFourthProject.Api.Authorization
{
    public static class AppPolicies
    {
        public const string UserOnly = nameof(UserOnly);
        public const string StaffOrManager = nameof(StaffOrManager);
        public const string ManagerOnly = nameof(ManagerOnly);
        public const string DriverOnly = nameof(DriverOnly);
    }
}
