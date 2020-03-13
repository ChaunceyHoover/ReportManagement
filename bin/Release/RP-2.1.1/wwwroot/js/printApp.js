var app = angular.module('ReportManagerPrint', ['ngMaterial', 'ngRoute', 'ngAnimate', 'ngMessages', 'ngAria']);

Date.prototype.yyyymmdd = function() {
	var mm = this.getMonth() + 1; // getMonth() is zero-based
	var dd = this.getDate();

	return [this.getFullYear(), (mm > 9 ? '' : '0') + mm, (dd > 9 ? '' : '0') + dd].join('-');
};

// Handles access token and allows user to stay logged in per browser
function accessTokenHttpInterceptor($window) {
	return {
		request: function(config) {
			var token = $window.localStorage.getItem('token');
			if (config.url.indexOf('/api') !== -1 && config.url !== '/api/auth/token') {
				config.headers['Authorization'] = 'Bearer ' + $window.localStorage.getItem('token');
			}
			return config;
		},
		response: function(response) {
			if (response.config.url === '/api/auth/token' && response.data.access_token) {
				if (response.status === 200) {
					$window.localStorage.setItem('token', response.data.access_token);
				} else {
					$window.localStorage.setItem('token', null);
				}
			}
			return response;
		}
	};
}
accessTokenHttpInterceptor.$inject = ['$window'];
function httpInterceptorRegistry($httpProvider) {
	$httpProvider.interceptors.push('accessTokenHttpInterceptor');
}
httpInterceptorRegistry.$inject = ['$httpProvider'];
app.config(httpInterceptorRegistry).factory('accessTokenHttpInterceptor', accessTokenHttpInterceptor);

app.service('UserInfo', function($http, $httpParamSerializerJQLike, $window) {
	this.updateToken = function() {
		var token = $window.localStorage.getItem('token');
		if (!!token) {
			var request = {
				"grant_type": "password",
				"username": "refresh",
				"password": "token"
			};
			$http({
				method: 'POST',
				url: '/api/auth/token',
				headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'Authorization': 'Bearer ' + token },
				data: $httpParamSerializerJQLike(request)
			});

			return true;
		}

		return false;
	}

	this.verify = function(callback) {
		$http({
			method: 'GET',
			url: '/api/auth',
		}).then(function(response) {
			callback(response, true);
		}).catch(function(response) {
			callback(response, false);
		});
	}
});

/// ** CONFIGURATION ** ///

// Disable caching (note: only doing this for dev build; prod build will cache)
app.config(['$httpProvider', function($httpProvider) {
	if (!$httpProvider.defaults.headers.get) {
		$httpProvider.defaults.headers.get = {};
	}

	$httpProvider.defaults.headers.get['Cache-Control'] = 'no-cache';
	$httpProvider.defaults.headers.get['Pragma'] = 'no-cache';
}]);

app.filter('percentage', ['$filter', function($filter) {
	return function(input, decimals) {
		return $filter('number')(input * 100, decimals) + '%';
	};
}]);

/// ** FACTORIES ** ///

app.factory('SitesService', function($http, $window) {
	return {
		reportList: function(startDate, endDate) {
			if (typeof (startDate) === "object")
				startDate = startDate.toJSON();
			if (typeof (endDate) === "object")
				endDate = endDate.toJSON();

			return $http({
				method: 'GET',
				url: '/api/sites',
				params: { 'start': startDate, 'end': endDate, 'status': 0 }
			});
		}
	}
});

app.controller('PrintCtrl', function($scope, $window, $http, $location, $routeParams, SitesService, UserInfo) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	var token = $window.localStorage.getItem('token');
	if (token == null)
		$window.location.href = "/login";

	$scope.sites = [];

	var getURIParams = function(variable) {
		var query = $window.location.search.substring(1);
		var vars = query.split('&');
		for (var i = 0; i < vars.length; i++) {
			var pair = vars[i].split('=');
			if (decodeURIComponent(pair[0]) == variable) {
				return decodeURIComponent(pair[1]);
			}
		}
		return null;
	};
	var startURI = getURIParams("start"), endURI = getURIParams("end"), system = getURIParams("system"), sort = getURIParams("sort");
	var start = new Date(startURI), end = new Date(endURI);
	$scope.sort = sort;

	SitesService.reportList(start.toJSON(), end.toJSON()).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.sites = response.data.report;
			sort = sort.toLowerCase();

			// Check for reverse sorting
			var reverse = false;
			if (sort.charAt(0) == '-') {
				reverse = true;
				sort = sort.substring(1);
			}

			// Filter out other systems
			if (system != null) {
				if (system != "(All)")
					for (var i = $scope.sites.length - 1; i >= 0; i--) {
						if ($scope.sites[i].systemName != system)
							$scope.sites.splice(i, 1);
					}
			}

			// Add in 'moneyHold' and 'moneyPercent'
			for (var i = 0; i < $scope.sites.length; i++) {
				var site = $scope.sites[i];
				site.moneyHold = site.moneyIn - site.moneyOut;
				site.moneyPercent = site.moneyHold / site.moneyIn;
			}

			if (sort == 'sitenumber') {
				// simple number sorting
				$scope.sites.sort(function(x, y) {
					if (x.siteNumber < y.siteNumber) return -1;
					if (x.siteNumber > y.siteNumber) return 1;
					return 0;
				});
			} else if (sort == 'distributor') {
				// Sort by distributor and site name, prioritizing distributor
				$scope.sites.sort(function(x, y) {
					var distrib1 = x.distributor, distrib2 = y.distributor;
					var name1 = x.siteName, name2 = y.siteName;

					if (distrib1 < distrib2) return -1;
					if (distrib1 > distrib2) return 1;
					if (name1 < name2) return -1;
					if (name1 > name2) return 1;
					return 0;
				});
			} else if (sort == 'systemname') {
				// Sort by system and site name, prioritizing system name
				$scope.sites.sort(function(x, y) {
					var sys1 = x.systemName, sys2 = y.systemName;
					var name1 = x.siteName, name2 = y.siteName;

					if (sys1 < sys2) return -1;
					if (sys1 > sys2) return 1;
					if (name1 < name2) return -1;
					if (name1 > name2) return 1;
					return 0;
				});
			} else if (sort == 'sitename') {
				// Sort by site name
				$scope.sites.sort(function(x, y) {
					if (x.siteName < y.siteName) return -1;
					if (x.siteName > y.siteName) return 1;
					return 0;
				});
			} else if (sort == 'moneyin') {
				$scope.sites.sort(function(x, y) {
					if (x.moneyIn < y.moneyIn) return -1;
					if (x.moneyIn > y.moneyIn) return 1;
					return 0;
				});
			} else if (sort == 'moneyout') {
				$scope.sites.sort(function(x, y) {
					if (x.moneyOut < y.moneyOut) return -1;
					if (x.moneyOut > y.moneyOut) return 1;
					return 0;
				});
			} else if (sort == 'moneyhold') {
				$scope.sites.sort(function(x, y) {
					if (x.moneyHold < y.moneyHold) return -1;
					if (x.moneyHold > y.moneyHold) return 1;
					return 0;
				});
			} else if (sort == 'moneypercent') {
				$scope.sites.sort(function(x, y) {
					if (x.moneyPercent < y.moneyPercent) return -1;
					if (x.moneyPercent > y.moneyPercent) return 1;
					return 0;
				});
			}
			if (reverse)
				$scope.sites.reverse();
		} else
			$scope.sites = [{ systemName: 'Error retrieving data', siteName: 'Please try again' }];
	});

	$scope.start = new Date(start);
	$scope.end = new Date(end);
	$http.get('/api/users?status=2').then(function(response) {
		if (response.data.status_code == 0)
			$scope.user = response.data.user.lName;
	});
});