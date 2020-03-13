var app = angular.module('ReportManager', ['ngMaterial', 'ngRoute', 'ngAnimate', 'md.data.table']);

/// ** CONFIGURATION ** ///

// Disable caching (note: only doing this for dev build; prod build will cache)
app.config(['$httpProvider', function($httpProvider) {
	if (!$httpProvider.defaults.headers.get) {
		$httpProvider.defaults.headers.get = {};
	}

	$httpProvider.defaults.headers.get['Cache-Control'] = 'no-cache';
	$httpProvider.defaults.headers.get['Pragma'] = 'no-cache';
}]);

// Color theming
app.config(function($mdThemingProvider) {
	$mdThemingProvider.theme('mainTheme')
		.primaryPalette('blue')
		.accentPalette('green')
		.warnPalette('red')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.setDefaultTheme('mainTheme');
});

// Icon name mapping
app.config(function($mdIconProvider) {
	$mdIconProvider
		.icon('add', '/img/icons/add.svg')
		.icon('approval', '/img/icons/approval.svg')
		.icon('back', '/img/icons/back-arrow.svg')
		.icon('close', '/img/icons/close.svg')
		.icon('date-range', '/img/icons/date-range.svg')
		.icon('edit', '/img/icons/edit.svg')
		.icon('exit', '/img/icons/exit.svg')
		.icon('home', '/img/icons/home.svg')
		.icon('list', '/img/icons/list.svg')
		.icon('logout', '/img/icons/logout.svg')
		.icon('notes', '/img/icons/notes.svg')
		.icon('paste', '/img/icons/paste.svg')
		.icon('profile', '/img/icons/profile.svg')
		.icon('settings', '/img/icons/settings.svg')
		.icon('user', '/img/icons/user.svg');
});

// Routing
app.config(function($routeProvider, $locationProvider) {
	$locationProvider.html5Mode(true);

	$routeProvider
		.when('/', {
			templateUrl: '/spa/overview.html',
			controller: 'IndexCtrl',
			title: 'Overview'
		})
		.when('/add-user', {
			templateUrl: '/spa/add-user.html',
			controller: 'AddUserCtrl',
			title: 'Create Users'
		})
		.when('/edit-user', {
			templateUrl: '/spa/edit-user.html',
			controller: 'EditUserCtrl',
			title: 'Modify Users'
		})
		.when('/account', {
			templateUrl: '/spa/account.html',
			controller: 'AccountCtrl',
			title: 'My Account'
		})
		.when('/site/:siteId', {
			templateUrl: '/spa/site.html',
			controller: 'SiteViewCtrl',
			title: 'Report Management'
		})
		.otherwise({
			redirectTo: '/'
		})
});

/// ** DIRECTIVES & FILTERS ** ///

// This filter makes the assumption that the input will be in decimal form (i.e. 17% is 0.17).
app.filter('percentage', ['$filter', function ($filter) {
	return function (input, decimals) {
		return $filter('number')(input * 100, decimals) + '%';
	};
}]);

app.directive('phoneInput', function($filter, $browser) {
	return {
		require: 'ngModel',
		link: function($scope, $element, $attrs, ngModelCtrl) {
			var listener = function() {
				var value = $element.val().replace(/[^0-9]/g, '');
				$element.val($filter('tel')(value, false));
			};

			// This runs when we update the text field
			ngModelCtrl.$parsers.push(function(viewValue) {
				return viewValue.replace(/[^0-9]/g, '').slice(0, 10);
			});

			// This runs when the model gets updated on the scope directly and keeps our view in sync
			ngModelCtrl.$render = function() {
				$element.val($filter('tel')(ngModelCtrl.$viewValue, false));
			};

			$element.bind('change', listener);
			$element.bind('keydown', function(event) {
				var key = event.keyCode;
				// If the keys include the CTRL, SHIFT, ALT, or META keys, or the arrow keys, do nothing.
				// This lets us support copy and paste too
				if (key === 91 || (15 < key && key < 19) || (37 <= key && key <= 40)) {
					return;
				}
				$browser.defer(listener); // Have to do this or changes don't get picked up properly
			});

			$element.bind('paste cut', function() {
				$browser.defer(listener);
			});
		}

	};
});
app.filter('tel', function() {
	return function(tel) {
		console.log(tel);
		if (!tel) { return ''; }

		var value = tel.toString().trim().replace(/^\+/, '');

		if (value.match(/[^0-9]/)) {
			return tel;
		}

		var country, city, number;

		switch (value.length) {
			case 1:
			case 2:
			case 3:
				city = value;
				break;

			default:
				city = value.slice(0, 3);
				number = value.slice(3);
		}

		if (number) {
			if (number.length > 3) {
				number = number.slice(0, 3) + '-' + number.slice(3, 7);
			}
			else {
				number = number;
			}

			return ("(" + city + ") " + number).trim();
		}
		else {
			return "(" + city;
		}

	};
});

/// ** FACTORIES ** ///

app.factory('BillingService', function($http, $window) {
	return {
        billing: function(siteId, startDate, endDate) {
            if (typeof(startDate) === "object")
                startDate = startDate.getFullYear() + "-" + (startDate.getMonth() < 9 ? "0" + startDate.getMonth() : startDate.getMonth())
                    + "-" + (startDate.getDate() < 9 ? "0" + startDate.getDate() : startDate.getDate());
            if (typeof(endDate) === "object")
                endDate = endDate.getFullYear() + "-" + (endDate.getMonth() < 9 ? "0" + endDate.getMonth() : endDate.getMonth())
                    + "-" + (endDate.getDate() < 9 ? "0" + endDate.getDate() : endDate.getDate());
            
            return $http({
                method: 'GET',
                url: '/api/bill/' + siteId,
                headers: { 'Authorization': 'Bearer ' + $window.sessionStorage.accessToken },
                params: { 'start': startDate, 'end': endDate }
            });
		}
	}
});

app.factory('SitesService', function($http, $window) {
    return {
        list: function(startDate, endDate, startIndex, stopIndex) {
            if (typeof (startDate) === "object")
                startDate = startDate.getFullYear() + "-" + (startDate.getMonth() < 9 ? "0" + startDate.getMonth() : startDate.getMonth())
                    + "-" + (startDate.getDate() < 9 ? "0" + startDate.getDate() : startDate.getDate());
            if (typeof (endDate) === "object")
                endDate = endDate.getFullYear() + "-" + (endDate.getMonth() < 9 ? "0" + endDate.getMonth() : endDate.getMonth())
                    + "-" + (endDate.getDate() < 9 ? "0" + endDate.getDate() : endDate.getDate());
            startIndex = startIndex || 0;
            stopIndex = stopIndex || 25;

            return $http({
                method: 'GET',
                url: '/api/sites',
                headers: { 'Authorization': 'Bearer ' + $window.sessionStorage.accessToken },
                params: { 'start': startDate, 'end': endDate, 'startIndex': startIndex, 'stopIndex': stopIndex }
            });
        },
        site: function(siteId) {
            return $http({
                method: 'GET',
                url: '/api/sites/' + siteId,
                headers: { 'Authorization': 'Bearer ' + $window.sessionStorage.accessToken }
            });
        }
    }
});

/// ** CONTROLLERS ** ///

// Main Page Controller
// url: /
app.controller('IndexCtrl', function ($scope, $window, $location, $mdSidenav, $mdDialog, BillingService, SitesService) {
	// Redirect if not logged in
	if ($window.sessionStorage.accessToken === null)
		$window.location.href = "/login";

	// Filtering
	$scope.today = new Date();
	$scope.filter = {};
	$scope.filter.dateRange = {
		start: new Date(),
		end: $scope.today
	}
	$scope.filter.dateRange.start.setDate($scope.today.getDate() - 7);

	$scope.getPercentColor = function(percent) {
		var targetRange = 0.3; // 30% is in the green
		var acceptance = 0.2; // +/- 20% is still good

		if (percent <= 0.5 && percent >= 0.2)
			return 'green';
		else if (percent < 0.1 || percent > 0.7)
			return 'red';
		else
			return 'amber';
	}

	// Footer should only show when needed
	$scope.showFooter = true;

	// Sidenav functions (open/close)
	$scope.toggleLeft = function () {
		$mdSidenav('left').toggle();
	}
	$scope.toggleRight = function () {
		$mdSidenav('right').toggle();
	}

	// SPA function
	$scope.changeSPA = function (page, ev) {
		switch (page) {
			case 1:
				$scope.toggleLeft();
				$mdDialog.show({
					controller: function ($scope, $mdDialog) {
						$scope.user = { 'level': 1 };
						$scope.hide = function () { $mdDialog.hide() }
						$scope.cancel = $scope.hide;
						$scope.create = function (ans) {
							if (verifyFields())
								$mdDialog.hide(ans);
							else
								$scope.errorMessage = "Please fill out all required fields.";
						}

						// Permission levels
						$scope.levels = [];
						for (var i = 1; i <= 10; i++) $scope.levels.push(i);

						function verifyFields() {
							// Required fields: (either first or last name), username, matching passwords, and phone #
							return ($scope.user.fname || $scope.user.lname) && $scope.user.username && $scope.user.password
								&& $scope.user.confirm_password && $scope.user.password === $scope.user.confirm_password && $scope.user.phone;
						}
					},
					templateUrl: 'tmpl/add-user.tmpl.html',
					parent: angular.element(document.body),
					targetEvent: ev,
					clickOutsideToClose: true
				}).then(function (response) {
					// create user
				});
				break;
			case 2:
				$scope.toggleLeft();
				$scope.showFooter = false;
				$location.path('/edit-user');
				break;
			case 3:
				$scope.toggleLeft();
				$scope.showFooter = false;
				// new tab => https://form.jotform.com/62645159795167
				break;
			case 4:
				$scope.toggleRight();
				$scope.showFooter = false;
				$location.path('/account');
				break;
			default:
				$scope.toggleLeft();
				$scope.showFooter = true;
				$location.path('/');
				break;
		}
	}


	// Dummy data for screenshot purposes
	//$scope.dummies = [
	//	{ 'id': 101, 'distributor': 'Franklin', 'system': 'STRYK3R', 'name': 'Inverness', 'in': 97792.16, 'out': 62349.13, 'active': true },
	//	{ 'id': 108, 'distributor': 'JP', 'system': 'STRYK3R', 'name': 'Hotspot - Wilson NC', 'in': 490937.81, 'out': 263429.34, 'active': true },
	//	{ 'id': 111, 'distributor': 'Elliot', 'system': 'STRYK3R', 'name': "Let's Play Vero Beach", 'in': 71402.25, 'out': 51164, 'active': true },
	//	{ 'id': 113, 'distributor': 'Elliot', 'system': 'STRYK3R', 'name': 'Winners', 'in': 301651.19, 'out': 196095.14, 'active': true },
	//	{ 'id': 115, 'distributor': 'Elliot', 'system': 'STRYK3R', 'name': 'Hidden Treasure', 'in': 221226.25, 'out': 154083.94, 'active': true },
	//	{ 'id': 116, 'distributor': 'Burkett 1', 'system': 'TYPHOON', 'name': 'Chiefland', 'in': 148192.6, 'out': 94440.35, 'active': true },
	//	{ 'id': 118, 'distributor': 'Skye', 'system': 'TYPHOON', 'name': 'AIA 2', 'in': 1412276.43, 'out': 996822.06, 'active': true },
	//	{ 'id': 127, 'distributor': 'Burkett 1', 'system': 'TYPHOON', 'name': 'Smoking & Game CTR (Ardyle)', 'in': 78977.9, 'out': 38257.11, 'active': true },
	//	{ 'id': 136, 'distributor': 'Burkett 1', 'system': 'TYPHOON', 'name': 'Duck Palace', 'in': 789456.2, 'out': 38257.11, 'active': true },
	//	{ 'id': 146, 'distributor': 'Burkett 1', 'system': 'TYPHOON', 'name': 'AIA', 'in': 1516178.51, 'out': 75000, 'active': true },
	//	{ 'id': 138, 'distributor': 'Burkett 1', 'system': 'TYPHOON', 'name': 'AIA 3', 'in': 18954, 'out': 12486.25, 'active': true }
	//];
    SitesService.list('2016-01-01', '2018-01-01', 0, 25).then(function(response) {
        $scope.sites = response.data.report;
        console.log($scope.dummies);
    })
	$scope.dummy = 0;
	$scope.searchText = "";
	$scope.searchTextChange = function(text) {

	}
	$scope.systems = ['101franklinstrykerinverness', '108jpstrykerhotspot - wilson nc', '111elliotstrykerlet\'s play vero beach', '113elliotstrykerwinners',
		'115elliotstrykerhidden treasure', '116burkett 1typhoonchiefland', '118skyetyphoonaia 2', '127burkett 1typhoonsmoking & game ctrardyle', '136burkett 1typhoonduck palace',
		'146burkett 1typhoonaia', '138burkett 1typhoonaia 3'];
	$scope.searchQueryChanged = function() {
		if ($scope.searchText === "" || $scope.searchText === " ")
			for (var i = 0; i < $scope.dummies.length; i++)
				$scope.dummies[i].active = true;
		else {
			for (var i = 0; i < $scope.dummies.length; i++) {
				$scope.dummies[i].active = $scope.systems[i].indexOf($scope.searchText.toLowerCase()) >= 0;
			}
		}
	}
});

// User Creation Controller
// url: /add-user
app.controller('AddUserCtrl', function($scope, $window) {
	
});

// User Modification Controller
// url: /edit-user
app.controller('EditUserCtrl', function($scope, $window) {

});

// User Account Controller
// url: /account
app.controller('AccountCtrl', function($scope, $window) {
	$scope.account = {
		'fname': 'Flamingo', 'lname': 'Sweeps',
		'phoneNumber': 7777777777,
		'email': 'na@na.com'
	}
});

app.controller('SiteViewCtrl', function($scope, $window) {
	$scope.dummy = 1;
	$scope.dummy2 = 0;
	$scope.distributors = [
		{ 'name': 'anthony Frank', 'id': 1 },
		{ 'name': 'Elliot', 'id': 2 },
		{ 'name': 'Burkett 1', 'id': 3 },
		{ 'name': 'Burkett 2', 'id': 4 }
	];
	$scope.systems = [
		{ 'name': 'STRYK3R', 'id': 0 },
		{ 'name': 'TYPHOON', 'id': 1 }
	];
});