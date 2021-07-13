using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using test_project_01.Models;
using test_project_01.Repositories;

namespace test_project_01.Services {
    public class ApiService {
        private readonly ProfileRepository profileRepository;
        private readonly ProviderRepository providerRepository;

        public ApiService(ProfileRepository profileRepository, ProviderRepository providerRepository) {
            this.profileRepository = profileRepository;
            this.providerRepository = providerRepository;
        }

        public async Task<IEnumerable<ProviderLocation>> GetItemsAsync(ProviderLocationHandle[] handlers) {
            var result = new List<ProviderLocation>();
            var providerIds = handlers
                .Select(plh => plh.ProviderId)
                .ToArray();
            var providers = Task.FromResult(await profileRepository.FindAsync(providerIds)).Result;
            var activeProfileIds = providers
                .Select(p => p.ActiveProfileId)
                .ToArray();
            var profiles = Task.FromResult(await providerRepository.FindAsync(activeProfileIds)).Result;
            foreach(var handler in handlers) {
                var provider = providers
                    .Where(p => p.Id == handler.ProviderId)
                    .FirstOrDefault();
                result.Add(GetProviderLocation(handler, provider, profiles));
            }
            return result;
        }
        ProviderLocation GetProviderLocation(ProviderLocationHandle handler, Provider provider, IEnumerable<Profile> profiles) {
            ProviderLocation providerLocation = new();
            providerLocation.ProviderId = handler.ProviderId;
            if(provider == null) {
                providerLocation.Status = ProviderLocationStatus.ProvidrNotFound;
                return providerLocation;
            }
            var profile = profiles
                .Where(p => p.ProviderId == handler.ProviderId)
                .FirstOrDefault();
            if(profile == null) {
                providerLocation.Status = ProviderLocationStatus.InvalidProfile;
                return providerLocation;
            }
            providerLocation.Profile = profile;
            var location = profiles
                .SelectMany(p => p.Locations
                    .Where(l => l.code.Equals(handler.LocationCode, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();
            if(location == null) {
                var firstLocation = profile.Locations.FirstOrDefault();
                providerLocation.LocationCode = firstLocation?.code;
                providerLocation.Location = firstLocation;
                providerLocation.Status = ProviderLocationStatus.LocationNotFound;
            } else {
                providerLocation.LocationCode = handler.LocationCode;
                providerLocation.Location = location;
                providerLocation.Status = ProviderLocationStatus.Found;
                providerLocation.Found = true;
            }
            return providerLocation;
        }
    }
}
