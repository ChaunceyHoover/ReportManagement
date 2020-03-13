var app = angular.module('ReportManager', ['ngMaterial', 'ngRoute', 'ngAnimate', 'ngMessages', 'ngAria', 'angular-loading-bar', 'md.data.table', 'mdPickers', 'ngCookies', 'ngMdBadge']);
var globalReport = {}, globalUserList;

// note to self: don't forget to update main.css's version as well
// (can't automate it due to fact that <head> tag is not in app's scope)
var _VERSION_NUMBER = 4;

Date.prototype.yyyymmdd = function() {
	var mm = this.getMonth() + 1; // getMonth() is zero-based
	var dd = this.getDate();

	return [this.getFullYear(), (mm > 9 ? '' : '0') + mm, (dd > 9 ? '' : '0') + dd].join('-');
};
String.prototype.format = function() {
	var args = arguments;
	return this.replace(/{(\d+)}/g, function(match, number) {
		return typeof args[number] != 'undefined'
			? args[number]
			: match
			;
	});
};

// https://stackoverflow.com/questions/6525538/convert-utc-date-time-to-local-date-time
function convertUTCDateToLocalDate(date) {
	var newDate = new Date(date.getTime() + date.getTimezoneOffset() * 60 * 1000);

	var offset = date.getTimezoneOffset() / 60;
	var hours = date.getHours();

	newDate.setHours(hours - offset);

	return newDate;
}

// Courtesy of SO; regex check for browser's user agent
// note: does not work for mobile requesting desktop due to device perfectly emulating desktop browser
//		 (only way to check past that point is screen resolution, and that isn't reliable w/ newer phones)
mobileAndTabletcheck = function() {
	var check = false;
	(function(a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
	return check;
};

// Handles access token and allows user to stay logged in per browser
function accessTokenHttpInterceptor($window) {
	return {
		request: function(config) {
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
// Color theming
app.config(function($mdThemingProvider) {
	var customBlue = $mdThemingProvider.extendPalette('blue', {
		'500': '#608fb4'
	});
	var customRed = $mdThemingProvider.extendPalette('red', {
		'500': '#d35f57'
	});
	var customYellow = $mdThemingProvider.extendPalette('yellow', {
		'500': '#dbce62',
		'contrastDefaultColor': 'light'
	});
	var customGreen = $mdThemingProvider.extendPalette('green', {
		'500': '#528e55',
		'A100': '#81C784'
	});
	var customPurple = $mdThemingProvider.extendPalette('deep-purple', {
		'500': '#6e51a3'
	});

	var accentGrey = $mdThemingProvider.extendPalette('blue-grey', {
		'A100': '#99aeb8'
	});
	$mdThemingProvider.definePalette('customBlue', customBlue);
	$mdThemingProvider.definePalette('customYellow', customYellow);
	$mdThemingProvider.definePalette('customRed', customRed);
	$mdThemingProvider.definePalette('customGreen', customGreen);
	$mdThemingProvider.definePalette('customPurple', customPurple);
	$mdThemingProvider.definePalette('accentGrey', accentGrey);

	$mdThemingProvider.theme('blueTheme')
		.primaryPalette('customBlue')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('yellowTheme')
		.primaryPalette('customYellow')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('redTheme')
		.primaryPalette('customRed')
		.accentPalette('blue')
		.warnPalette('customYellow')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('greenTheme')
		.primaryPalette('customGreen')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('purpleTheme')
		.primaryPalette('customPurple')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('greyTheme')
		.primaryPalette('accentGrey')
		.accentPalette('accentGrey')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.setDefaultTheme('blueTheme');
	$mdThemingProvider.alwaysWatchTheme(true); // need this enabled to set color from config
});
// Fix for double request sending(?)
app.config(function($mdGestureProvider) {
	$mdGestureProvider.skipClickHijack();
});
// Icon name mapping
app.config(function($mdIconProvider) {
	$mdIconProvider
		.icon('add', '/img/icons/add.svg')
		.icon('approval', '/img/icons/approval.svg')
		.icon('back', '/img/icons/back-arrow.svg')
		.icon('chevron-left', '/img/icons/chevron-left.svg')
		.icon('chevron-right', '/img/icons/chevron-right.svg')
		.icon('close', '/img/icons/close.svg')
		.icon('date-range', '/img/icons/date-range.svg')
		.icon('desktop', '/img/icons/desktop.svg')
		.icon('disabled', '/img/icons/disabled.svg')
		.icon('download', '/img/icons/download.svg')
		.icon('edit', '/img/icons/edit.svg')
		.icon('exit', '/img/icons/exit.svg')
		.icon('face', '/img/icons/face.svg')
		.icon('first-page', '/img/icons/first-page.svg')
		.icon('home', '/img/icons/home.svg')
		.icon('last-page', '/img/icons/last-page.svg')
		.icon('list', '/img/icons/list.svg')
		.icon('logout', '/img/icons/logout.svg')
		.icon('log', '/img/icons/logs.svg')
		.icon('money', '/img/icons/money.svg')
		.icon('notes', '/img/icons/notes.svg')
		.icon('paste', '/img/icons/paste.svg')
		.icon('print', '/img/icons/print.svg')
		.icon('profile', '/img/icons/profile.svg')
		.icon('settings', '/img/icons/settings.svg')
		.icon('site', '/img/icons/site.svg')
		.icon('status-icon', '/img/icons/status-icon.svg')
		.icon('ticket', '/img/icons/ticket.svg')
		.icon('user', '/img/icons/user.svg')
		.icon('warning', '/img/icons/warning.svg');
});
// Routing
app.config(function($routeProvider, $locationProvider) {
	$locationProvider.html5Mode(true);

	$routeProvider
		.when('/', {
			templateUrl: '/spa/overview.html?' + _VERSION_NUMBER,
			controller: 'IndexCtrl',
			title: 'Overview'
		})
		.when('/add-user', { // must be permission level 2 or 10
			templateUrl: '/spa/add-user.html?' + _VERSION_NUMBER,
			controller: 'AddUserCtrl',
			title: 'Create Users'
		})
		.when('/edit-user', { // must be permission level 2 or 10
			templateUrl: '/spa/edit-user.html?' + _VERSION_NUMBER,
			controller: 'EditUserCtrl',
			title: 'Modify Users'
		})
		.when('/edit-user/:userId', { // must be permission level 2 or 10
			templateUrl: '/spa/user.html?' + _VERSION_NUMBER,
			controller: 'UserViewCtrl',
			title: 'Modify User'
		})
		.when('/account', {
			templateUrl: '/spa/account.html?' + _VERSION_NUMBER,
			controller: 'AccountCtrl',
			title: 'My Account'
		})
		.when('/site/:siteId', {
			templateUrl: '/spa/site.html?' + _VERSION_NUMBER,
			controller: 'SiteViewCtrl',
			title: 'Site Details'
		})
		.when('/activate', {
			templateUrl: '/spa/activate-site.html?' + _VERSION_NUMBER,
			controller: 'ActivateSiteCtrl',
			title: 'Activate Site'
		})
		.when('/disable', {
			templateUrl: '/spa/disabled-sites.html?' + _VERSION_NUMBER,
			controller: 'DisabledSitesCtrl',
			title: 'Disabled Sites'
		})
		.when('/logs', {
			templateUrl: '/spa/logs.html?' + _VERSION_NUMBER,
			controller: 'LogsCtrl',
			title: 'Logs'
		})
		.when('/reactivate/:formId', {
			templateUrl: '/spa/reactivate.html?' + _VERSION_NUMBER,
			controller: 'ReactivateCtrl',
			title: 'Re-Activate Site'
		})
		.when('/preapprove', {
			templateUrl: '/spa/reactivate.html?' + _VERSION_NUMBER,
			controller: 'PreapprovedCtrl',
			title: 'Pre-Approve Site'
		})
		.when('/notifications', {
			templateUrl: '/spa/notifications.html?' + _VERSION_NUMBER,
			controller: 'NotificationCtrl',
			title: 'Notifications'
		})
		.when('/adjustments', {
			templateUrl: '/spa/adjustments.html?' + _VERSION_NUMBER,
			controller: 'AdjustmentsCtrl',
			title: 'Adjustments'
		})
		.when('/tickets', {
			templateUrl: '/spa/tickets.html?' + _VERSION_NUMBER,
			controller: 'TicketsCtrl',
			title: 'Tickets'
		})
		.otherwise({
			redirectTo: '/'
		});
});
// Make angular not tag my URLs as unsafe
app.config([
	'$compileProvider',
	function($compileProvider) {
		$compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|ftp|mailto|chrome-extension|javascript):/);
		// Angular before v1.2 uses $compileProvider.urlSanitizationWhitelist(...)
	}
]);

// Change page title on successful route change
app.run(['$rootScope', '$route', function($rootScope, $route) {
	$rootScope.$on('$routeChangeSuccess', function() {
		document.title = "RP - " + $route.current.title;
	});
}]);

/// ** DIRECTIVES & FILTERS ** ///

// This filter makes the assumption that the input will be in decimal form (i.e. 17% is 0.17).
app.directive('format', ['$filter', function($filter) {
	return {
		require: '?ngModel',
		link: function(scope, elem, attrs, ctrl) {
			if (!ctrl) return;

			ctrl.$formatters.unshift(function(a) {
				return $filter(attrs.format)(ctrl.$modelValue)
			});

			elem.bind('blur', function(event) {
				var plainNumber = elem.val().replace(/[^\d|\-+|\.+]/g, '');
				elem.val($filter(attrs.format)(plainNumber));
			});
		}
	};
}]);
app.filter('percentage', ['$filter', function($filter) {
	return function(input, decimals) {
		return !input ? "0%" : $filter('number')(input * 100, decimals) + '%';
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
app.filter('startFrom', function() {
	return function(input, start) {
		start = +start; //parse to int
		return input.slice(start);
	}
});
app.directive('resize', ['$window', function($window) {
	return {
		link: link,
		restrict: 'A'
	};
	function link(scope, element, attrs) {
		scope.width = $window.innerWidth;

		function onResize() {
			// uncomment for only fire when $window.innerWidth change   
			if (scope.width !== $window.innerWidth) {
				scope.width = $window.innerWidth;
				scope.$digest();
			}
		};

		function cleanUp() {
			angular.element($window).off('resize', onResize);
		}

		angular.element($window).on('resize', onResize);
		scope.$on('$destroy', cleanUp);
	}
}]);
app.filter('filterUser', function() {
	return function(items, userArray) {
		var filtered = [];
		if (userArray.length == 0) return items;
		angular.forEach(items, function(item) {
			angular.forEach(userArray, function(userId) {
				if (item.userId == userId)
					filtered.push(item);
			});
		});
		return filtered;
	}
});
app.filter('filterAction', function() {
	return function(items, actionArray) {
		var filtered = [];
		if (actionArray.length == 0) return items;
		angular.forEach(items, function(item) {
			angular.forEach(actionArray, function(actionId) {
				if (item.action == actionId)
					filtered.push(item);
			});
		});
		return filtered;
	}
});
app.filter('filterYourNotifications', function() {
	return function(items, id) {
		if (id == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (item.claimedById == id)
				filtered.push(item);
		});
		return filtered;
	}
});
app.filter('filterNotificationType', function() {
	return function(items, type) {
		if (type == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (item.notificationType == type)
				filtered.push(item);
		});
		return filtered;
	}
});
// Filters for adjustment page
app.filter('filterClaimed', function() {
	return function(items, claimed) {
		if (claimed == false || claimed == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (item.claimedById == 0)
				filtered.push(item);
		});
		return filtered;
	}
});
app.filter('filterCompleted', function() {
	return function(items, completed) {
		if (completed == false || completed == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (item.completed == false)
				filtered.push(item);
		});
		return filtered;
	}
});
// middle click directive
app.directive('ngMiddleClick', function() {
	var e = {
		restrict: "A",
		link: function(e, t, n) {
			var i = n.ngMiddleClick || n.ngClick;
			if (i) {
				var c = "onauxclick" in document.documentElement ? "auxclick" : "mousedown";
				t.on(c, function(t) {
					if (2 === t.which) {
						if ("disabled" === t.currentTarget.getAttribute("disabled")) return t.preventDefault();
						e.$eval(i, {
							$event: t
						})
					}
				})
			}
		}
	};
	return e
});
// Filters for ticket page
app.filter('filterAssignedToYou', function() {
	return function(items, assigned) {
		if (items == false || items == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {

		});
		return filtered;
	}
});
app.filter('filterCreatedByYou', function() {
	return function(items, show, id) {
		if (items == false || items == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (!show)
				filtered.push(item);
			else
				if (item.createdBy == id || item.assignedTo == id)
					filtered.push(item);
		});
		return filtered;
	}
});
app.filter('filterShowClosedTickets', function() {
	return function(items, show) {
		if (items == false || items == null) return items;

		var filtered = [];
		angular.forEach(items, function(item) {
			if (show)
				filtered.push(item);
			else
				if (item.status != 4)
					filtered.push(item);
		});
		return filtered;
	}
});

/// ** FACTORIES ** ///

app.factory('ActivationService', function($http, $cookies, UserInfo) {
	return {
		deleteActivation: function(formId) {
			UserInfo.updateToken();
			return $http({
				method: 'DELETE',
				url: '/api/activate/' + formId
			});
		},
		getActivation: function(siteId) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/activate/' + siteId,
				params: { status: 0 }
			});
		},
		getForm: function(formId) {
			UserInfo.updateToken(); // you know what it is, bitch
			return $http({
				method: 'GET',
				url: '/api/activate/' + formId,
				params: { status: 1 }
			});
		},
		getUnapproved: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/activate'
			});
		},
		approve: function(form) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'POST',
				url: '/api/activate',
				data: form
			});
		},
		activate: function(formattedBody) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'PUT',
				url: '/api/activate',
				data: formattedBody,
				params: { type: 0 }
			});
		},
		reactivate: function(formattedBody) {
			return $http({
				method: 'PUT',
				url: '/api/activate',
				data: formattedBody,
				params: { type: 1 }
			});
		}
	}
});
app.factory('AdjustmentService', function($http, $cookies, UserInfo) {
	return {
		claim: function(adjustmentId) {
			return $http({
				method: 'POST',
				url: '/api/adjust/' + adjustmentId
			});
		},
		deleteAdjustment: function(adjustmentId) {
			UserInfo.updateToken();
			return $http({
				method: 'DELETE',
				url: '/api/adjust/' + adjustmentId
			});
		},
		getAdjustments: function(siteId) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/adjust/' + siteId,
				params: { status: 0 }
			});
		},
		getAdjustmentTypes: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/adjust',
				params: { status: 2 }
			});
		},
		getAll: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/adjust',
				params: { 'status': 1 }
			});
		},
		getUnfinished: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/adjust',
				params: { 'status': 0 }
			});
		},
		getCode: function(siteId) {
			UserInfo.updateToken();
			return $http({
				metohd: 'GET',
				url: '/api/adjust/' + siteId,
				params: { status: 1 }
			});
		},
		submit: function(adjustment) {
			UserInfo.updateToken();
			return $http({
				method: 'PUT',
				url: '/api/adjust',
				data: adjustment
			});
		},
		submitBulk: function(adjustment, list) {
			UserInfo.updateToken();
			console.log(adjustment, list);
			return $http({
				method: 'POST',
				url: '/api/adjust',
				data: {
					'DelimitedSites': list,
					'BulkAdjustment': adjustment
				}
			});
		},
		verify: function(adjustment) {
			UserInfo.updateToken();
			return $http({
				method: 'POST',
				url: '/api/adjust',
				data: adjustment
			});
		}
	}
})
app.factory('AuthService', function($http, $cookies, UserInfo) {
	return {
		getAuthorization: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/auth'
			});
		}
	}
});
app.factory('InstallerService', function($http, $cookies, UserInfo) {
	return {
		getInstallers: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/installer'
			});
		},
		getInstaller: function(id) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/installer/' + id
			});
		},
		createInstaller: function(installer) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'PUT',
				url: '/api/installer',
				data: installer
			});
		}
	}
});
app.factory('LogService', function($http, $cookies, UserInfo) {
	return {
		getLogs: function(start, end) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/logs?start=' + start.toJSON() + "&end=" + end.toJSON()
			});
		}
	}
})
app.factory('ManagerService', function($http, $cookies, UserInfo) {
	return {
		getManagers: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/manager'
			});
		},
		createManager: function(manager) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'PUT',
				url: '/api/manager',
				data: manager
			});
		}
	}
});
app.factory('PermissionsService', function($http, $cookies, UserInfo) {
	return {
		getPermission: function(userId) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/permission/' + userId
			});
		},
		getLinks: function() {
			UserInfo.updateToken();
			return $http({
				metohd: 'GET',
				url: '/api/permission/links'
			});
		},
		getTheme: function() {
			return $http({
				method: 'GET',
				url: '/api/permission/theme'
			});
		},
		setPermission: function(userId, siteId, access) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'POST',
				url: '/api/permission',
				params: { userId: userId, siteId: siteId, access: access ? 1 : 0 }
			});
		}
	}
});
app.factory('PlayerAdjustmentService', function($http, $cookies, UserInfo) {
	return {
		claim: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'PUT',
				url: "/api/pins/" + id,
				headers: {
					"Content-Type": "application/json"
				},
				params: { status: 0 },
				data: ''
			});
		},
		complete: function(id, code) {
			UserInfo.updateToken();
			return $http({
				method: 'PUT',
				url: "/api/pins/" + id,
				data: { verification: code },
				headers: {
					"Content-Type": "application/json"
				},
				params: { status: 1 }
			});
		},
		create: function(adjustment) {
			UserInfo.updateToken();
			return $http({
				method: 'POST',
				url: '/api/pins',
				data: adjustment
			});
		},
		delete: function(adjustment) {
			UserInfo.updateToken();
			return $http({
				method: 'DELETE',
				url: '/api/pins/' + adjustment.id
			});
		},
		getAllAdjustments: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/pins',
				params: { status: 1 }
			});
		},
		getUnclaimedAdjustments: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/pins',
				params: { status: 0 }
			});
		},
		getAdjustment: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/pins/' + id,
				params: { 'status': 0 }
			});
		},
		getSiteAdjustments: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/pins/' + id,
				params: { 'status': 1 }
			});
		}
	}
});
app.factory('PlayersService', function($http, $cookies, UserInfo) {
	return {
		listAll: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/players/' + id,
				params: { status: 1 }
			});
		},
		player: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/players/' + id,
				params: { status: 0 }
			});
		}
	}
});
app.factory('SitesService', function($http, $cookies, UserInfo) {
	return {
		bulkUpdateDistribs: function(siteList, distribId) {
			UserInfo.updateToken();
			return $http({
				method: 'PUT',
				url: '/api/sites',
				data: {
					'SiteList': siteList,
					'Distributor': { id: distribId }
				}
			});
		},
		disabledSites: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/sites',
				params: { 'status': 2 }
			});
		},
		listAll: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/sites',
				params: { 'status': 1 }
			});
		},
		reportList: function(startDate, endDate) {
			UserInfo.updateToken(); // Updates user access token
			if (typeof (startDate) === "object")
				startDate = startDate.getFullYear() + "-" + (startDate.getMonth() < 9 ? "0" + startDate.getMonth() : startDate.getMonth())
					+ "-" + (startDate.getDate() < 9 ? "0" + startDate.getDate() : startDate.getDate());
			if (typeof (endDate) === "object")
				endDate = endDate.getFullYear() + "-" + (endDate.getMonth() < 9 ? "0" + endDate.getMonth() : endDate.getMonth())
					+ "-" + (endDate.getDate() < 9 ? "0" + endDate.getDate() : endDate.getDate());

			return $http({
				method: 'GET',
				url: '/api/sites',
				params: { 'start': startDate, 'end': endDate, 'status': 0 }
			});
		},
		site: function(siteId) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/sites/' + siteId
			});
		},
		update: function(newInfo) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'POST',
				url: '/api/sites',
				data: newInfo
			});
		}
	}
});
app.factory('SystemService', function($http, $cookies, UserInfo) {
	return {
		list: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/systems'
			});
		},
		system: function(id) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/systems/' + id
			});
		}
	}
});
app.factory('TicketService', function($http, $cookies, UserInfo) {
	return {
		deleteTicket: function(ticketId) {
			UserInfo.updateToken();
			return $http({
				method: 'DELETE',
				url: '/api/ticket/' + ticketId
			});
		},
		getAllTickets: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/ticket?status=0'
			});
		},
		getSiteTickets: function(siteId) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/ticket?status=2&id=' + siteId
			});
		},
		submitTicket: function(ticket) {
			UserInfo.updateToken();
			return $http({
				method: 'PUT',
				url: '/api/ticket',
				data: ticket
			});
		},
		updateTicket: function(ticket) {
			UserInfo.updateToken();
			return $http({
				method: 'POST',
				url: '/api/ticket',
				data: ticket
			});
		}
	}
});
app.factory('TicketCommentService', function($http, $cookies, UserInfo) {
	return {
		getTicketComments: function(ticketId) {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/comments/' + ticketId
			});
		},
		createTicketComment: function(comment) {
			UserInfo.updateToken();
			return $http({
				method: 'POST',
				url: '/api/comments',
				data: comment
			});
		}
	}
});
app.factory('UserService', function($http, $cookies, UserInfo) {
	return {
		create: function(user) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'PUT',
				url: '/api/users',
				params: { 'status': 0 },
				data: user
			});
		},
		delete: function(userId) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'PUT',
				url: '/api/users',
				params: { 'status': 1 },
				data: { 'Id': userId }
			});
		},
		distributors: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/users',
				params: { 'status': 0 }
			});
		},
		highLevelUsers: function() {
			UserInfo.updateToken();
			return $http({
				method: 'GET',
				url: '/api/users',
				params: { 'status': 4 }
			});
		},
		levels: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/users/roles'
			});
		},
		owners: function() {
			return $http({
				method: 'GET',
				url: '/api/users',
				params: { status: 3 }
			});
		},
		user: function(id) {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/users/' + id
			});
		},
		users: function() {
			UserInfo.updateToken(); // Updates user access token
			return $http({
				method: 'GET',
				url: '/api/users',
				params: { 'status': 1 }
			});
		},
		update: function(newInfo, updatePwd) {
			UserInfo.updateToken(); // Updates user access token
			if (updatePwd == true)
				return $http({
					method: 'POST',
					url: '/api/users',
					params: { status: 1 },
					data: newInfo
				});
			else
				return $http({
					method: 'POST',
					url: '/api/users',
					params: { status: 0 },
					data: newInfo
				});

		}
	}
});

/// ** CONTROLLERS ** ///

// Main Page Controller
// url: /
app.controller('IndexCtrl', function($scope, $filter, $window, $location, $mdMenu, $mdSidenav, $mdDialog, $mdToast,
	ActivationService, AdjustmentService, AuthService, SitesService, SystemService, UserService, PermissionsService, PlayerAdjustmentService, TicketService, UserInfo) {
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login?redirectTo=/";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	$scope.tabs = { index: 0 };

	$scope.reportGenerated = false;
	$scope.theme = 'purpleTheme';
	PermissionsService.getTheme().then(function(response) {
		$scope.theme = response.data.theme.toLowerCase() + "Theme";
	});

	$scope.desktopView = false;
	$scope.forceDesktopView = function() {
		$(window).resize(function() {
			$('meta[name=viewport]').attr('content', 'width=1200');
		}).resize();
		$scope.toggleRight();
	}
	$scope.mobileCheck = mobileAndTabletcheck;

	$scope.timeSupported = Modernizr.inputtypes.time;
	setTimeout(function() {
		$('#start-time, #end-time').timepicker();
		$('#mobile-start-time, #mobile-end-time').timepicker();
	}, 1000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	UserInfo.updateToken(); // Updates user access token

	// Get permission levels & verify against user's level
	PermissionsService.getLinks().then(function(response) {
		if (response.data.status_code == 0) { // plot twist: it's always 0
			$scope.addUserLevel = response.data.addUser;
			$scope.modifyUserLevel = response.data.modifyUser;
			$scope.addSiteLevel = response.data.addSite;
			$scope.adjustmentLevel = response.data.adjustments;
			$scope.sidenavLinks = response.data.links;
			updatePermissions();
		}
	});
	AuthService.getAuthorization().then(function(response) {
		$scope.permissionLevel = response.data.level;
		$scope.user = { id: response.data.subject, name: response.data.name };
		updatePermissions();
	});

	function canCreateUser() {
		if ($scope.permissionLevel == null) {
			$scope.canCreateUser = false;
			return;
		}
		if ($scope.addUserLevel == null) {
			$scope.canCreateUser = true;
			return;
		}

		for (var i = 0; i < $scope.addUserLevel.length; i++)
			if ($scope.addUserLevel[i] == $scope.permissionLevel)
				$scope.canCreateUser = true;
	}
	function canEditUser() {
		if ($scope.permissionLevel == null) {
			$scope.canEditUser = false;
			return;
		}
		if ($scope.modifyUserLevel == null) {
			$scope.canEditUser = true;
			return;
		}

		for (var i = 0; i < $scope.modifyUserLevel.length; i++)
			if ($scope.modifyUserLevel[i] == $scope.permissionLevel)
				$scope.canEditUser = true;
	}
	function canCreateSite() {
		if ($scope.permissionLevel == null) {
			$scope.canCreateSite = false;
			return;
		}
		if ($scope.addSiteLevel == null) {
			$scope.canCreateSite = true;
			return;
		}

		for (var i = 0; i < $scope.addSiteLevel.length; i++)
			if ($scope.addSiteLevel[i] == $scope.permissionLevel)
				$scope.canCreateSite = true;
	}
	function canAdjust() {
		if ($scope.permissionLevel == null || $scope.adjustmentLevel == null) {
			$scope.canDoAdjustments = false;
			return;
		}
		// will never be open for all to do, so no need to check
		for (var i = 0; i < $scope.adjustmentLevel.length; i++)
			if ($scope.adjustmentLevel[i] == $scope.permissionLevel)
				$scope.canDoAdjustments = true;
	}
	function updatePermissions() {
		canCreateUser();
		canEditUser();
		canCreateSite();
		canAdjust();
	}
	$scope.checkLinkPermission = function(link) {
		if ($scope.permissionLevel == null) return false;
		if (link.level == null) return true;

		for (var i = 0; i < link.level.length; i++)
			if ($scope.permissionLevel == link.level[i])
				return true;
		return false;
	}

	// Prevent spam report generation
	$scope.canGenerateReport = true;

	// Approval menu
	var originatorEv;
	this.openMenu = function($mdOpenMenu, ev) {
		originatorEv = ev;
		$mdOpenMenu(ev);
	};

	// Notification menu
	$scope.openNotifications = function() {
		window.location = "/notifications";
		getNotifications();
	}
	$scope.openTickets = function() {
		window.location = "/tickets";
		getNotifications();
	}

	function getNotifications() {
		$scope.activations = [];
		$scope.adjustments = [];
		$scope.playerAdjustments = [];
		$scope.tickets = [];
		ActivationService.getUnapproved().then(function(response) {
			if (response.data.status_code == 0) {
				$scope.activations = response.data.report;
			}
		});
		AdjustmentService.getUnfinished().then(function(response) {
			if (response.data.status_code == 0) {
				$scope.adjustments = response.data.report;
			}
		});
		PlayerAdjustmentService.getUnclaimedAdjustments().then(function(response) {
			if (response.data.status_code == 0)
				$scope.playerAdjustments = response.data.report;
		});
		TicketService.getAllTickets().then(function(response) {
			if (response.data.status_code == 0) {
				var t = [];
				angular.forEach(response.data.report, function(ticket) {
					if (ticket.status != 4)
						t.push(ticket);
				});
				$scope.tickets = t;
			}
		});
	}
	setInterval(getNotifications, 30000);
	getNotifications();
	$scope.$on('$routeChangeStart', getNotifications);

	// Filtering
	$scope.today = Date.today();
	$scope.filter = {
		checkAll: false,
		selectedSystem: "(All)"
	};
	$scope.filter.dateRange = {
		start: Date.today().addDays(-7),
		startTime: new Date("2000-01-01 03:00:00"),
		end: Date.today().addDays(1),
		endTime: new Date("2000-01-01 03:00:00")
	}

	$scope.checkAllFilter = function() {
		angular.forEach($scope.sites, function(site) {
			site.checkBox = $scope.filter.checkAll || false;
		});
		$scope.showBulkActions = $scope.filter.checkAll || false;
	}
	$scope.siteFilterChecked = function() {
		var shown = false;
		angular.forEach($scope.sites, function(site) {
			if (site.checkBox) {
				$scope.showBulkActions = true;
				shown = true;
				return;
			}
		});
		if (!shown)
			$scope.showBulkActions = false;
		var same = $scope.sites[0].checkBox;
		for (var i = 1; i < $scope.sites.length; i++) {
			if ($scope.sites[i].checkBox != same) {
				same = false;
				break;
			}
		}
		$scope.filter.checkAll = same;
	}

	$scope.bulkAdjustment = function(ev) {
		$mdDialog.show({
			controller: BulkAdjustmentCtrl,
			templateUrl: 'tmpl/bulk-adjustment.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			if (response) {
				var siteList = [];
				angular.forEach($scope.sites, function(site) {
					if (site.checkBox)
						siteList.push(site.id);
				});

				$mdToast.show(
					$mdToast.simple()
						.textContent("Submitting & verifyinf adjustments, this may take a moment...")
						.position("top right")
						.hideDelay(4000)
				);
				AdjustmentService.submitBulk(response, siteList.join(',')).then(function(bulkResponse) {
					if (bulkResponse.data.status_code == 0)
						$mdToast.show(
							$mdToast.simple()
								.textContent("All adjustments successfully submitted")
								.position("top right")
								.hideDelay(3000)
						);
					else
						$mdToast.show(
							$mdToast.simple()
								.textContent("[" + bulkResponse.data.status_code + "] " + bulkResponse.data.status)
								.position("top right")
								.hideDelay(3000)
						);
				});
			}
		});
	}
	$scope.bulkSetDistributor = function(ev) {
		$mdDialog.show({
			controller: BulkSetDistribCtrl,
			templateUrl: 'tmpl/bulk-set-distrib.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			if (response) {
				var siteList = [];
				angular.forEach($scope.sites, function(site) {
					if (site.checkBox)
						siteList.push(site.id);
				});
				SitesService.bulkUpdateDistribs(siteList, response.id).then(function(bulkResponse) {
					if (bulkResponse.data.status_code == 0) {
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully set site distributors")
								.position("top right")
								.hideDelay(4000)
						);
						angular.forEach($scope.sites, function(site) {
							if (site.checkBox) {
								site.checkBox = false;
								site.distributor = response.name;
							}
						});
						$scope.filter.checkAll = false;
						$scope.showBulkActions = false;
					} else
						$mdToast.show(
							$mdToast.simple()
								.textContent("[" + bulkResponse.data.status_code + "] " + bulkResponse.data.status)
								.position("top right")
								.hideDelay(4000)
						);
				});
			}
		});
	}

	$scope.showBulkActions = false;

	// Pagination
	$scope.sites = []; // placeholder until HTTP GET fills list
	$scope.systems = [0];

	// Populates system filter list
	SystemService.list().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.systems = [{ id: 0, name: "(All)", prefix: null }];
			for (var i = 0; i < response.data.report.length; i++) {
				$scope.systems.push({ id: response.data.report[i].id, name: response.data.report[i].name, prefix: response.data.report[i].prefix });
			}
		} else
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		setTableHeight();
	});

	$scope.systemFilterChanged = function() {
		if (globalReport.report == null) return;
		$scope.sites = angular.extend([], globalReport.report);
		if ($scope.filter.selectedSystem != "(All)") {
			for (var i = $scope.sites.length - 1; i >= 0; i--)
				if ($scope.sites[i].systemName != $scope.filter.selectedSystem)
					$scope.sites.splice(i, 1);
		}
		setTableHeight();
	}

	$scope.query = {
		order: 'siteNumber',
		limit: 100000,
		page: 1
	}
	$scope.limitOptions = [25, 50, 75, 100, {
		label: 'All',
		value: function() {
			return $scope.sites ? $scope.sites.length : 0;
		}
	}];

	// Fixes bug: when on page 2+ and searching, if search result doesn't generate 2+ pages of search results,
	// no results will be rendered
	$scope.searchQueryChanged = function() {
		if ($scope.filter.searchText.trim() != "")
			$scope.query.page = 1;
	}

	$scope.previous = function() {
		if (($scope.query.page - 1) > 0)
			$scope.query.page = $scope.query.page - 1;
	}
	$scope.next = function() {
		if (($scope.query.page) * $scope.query.limit < $scope.sites.length)
			$scope.query.page = $scope.query.page + 1;
	}

	// Gets color value based off given percent
	$scope.getPercentColor = function(percent, mobile) {
		var targetRange = 0.3; // 30% is in the green
		var acceptance = 0.2; // +/- 20% is still good

		if (percent <= 0.5 && percent >= 0.2)
			return mobile ? 'green-300' : 'green';
		else if (percent < 0.1 || percent > 0.7)
			return mobile ? 'red-300' : 'red';
		else
			return mobile ? 'amber-200' : 'amber';
	}

	// Gets the site online status color based off last ping (>1hr = assumed to be offline)
	$scope.getSiteStatusColor = function(time) {
		return ($scope.today - new Date(time)) < 3600 ? 'green' : 'red';
	}

	// Mobile report
	$scope.toggleReport = function(id) {
		var report = document.getElementById("report" + id);
		if (report == null) return;
		report.classList.toggle('visible');
	}

	function setTo3AM() {
		setTimeout(function() {
			$('#start-time, #end-time').val('03:00');
			$('#mobile-start-time, #mobile-end-time').val('03:00');
			$('#material-start-time, #material-end-time').val('03:00:00.000');
		});
		$scope.filter.dateRange.start.setHours(3); $scope.filter.dateRange.start.setMinutes(0); $scope.filter.dateRange.start.setSeconds(0);
		$scope.filter.dateRange.end.setHours(3); $scope.filter.dateRange.end.setMinutes(0); $scope.filter.dateRange.end.setSeconds(0);
	}

	// Generates a report with pre-defined date ranges
	// 0 = weekly, 1 = monthly, 2 = quarterly, 3 = year-to-date, 4 = current billing week, 5 = previous billing week
	$scope.quickReport = function(type) {
		if ($scope.canGenerateReport) {
			$scope.filter.dateRange.start = new Date();
			$scope.filter.dateRange.end = new Date();
			switch (type) {
				case 1:
					$scope.filter.dateRange.start.setDate($scope.filter.dateRange.start.getDate() - 31);
					setTo3AM();
					break;
				case 2:
					var month = $scope.filter.dateRange.start.getMonth();
					if (month >= 0 && month <= 2) {
						$scope.filter.dateRange.start = new Date($scope.filter.dateRange.start.getFullYear() + "-01-01T03:00:00");
						$scope.filter.dateRange.end = new Date($scope.filter.dateRange.start.getFullYear() + "-03-31T03:00:00");
					} else if (month >= 3 && month <= 5) {
						$scope.filter.dateRange.start = new Date($scope.filter.dateRange.start.getFullYear() + "-04-01T03:00:00");
						$scope.filter.dateRange.end = new Date($scope.filter.dateRange.start.getFullYear() + "-06-30T03:00:00");
					} else if (month >= 6 && month <= 8) {
						$scope.filter.dateRange.start = new Date($scope.filter.dateRange.start.getFullYear() + "-07-01T03:00:00");
						$scope.filter.dateRange.end = new Date($scope.filter.dateRange.start.getFullYear() + "-09-30T03:00:00");
					} else {
						$scope.filter.dateRange.start = new Date($scope.filter.dateRange.start.getFullYear() + "-10-01T03:00:00");
						$scope.filter.dateRange.end = new Date($scope.filter.dateRange.start.getFullYear() + "-12-31T03:00:00"); // happy new year¡
					}
					break;
				case 3:
					var today = Date.today();
					$scope.filter.dateRange.start.setDate(today.getDate() - 364);
					$scope.filter.dateRange.end.setDate(today.getDate() + 1);
					setTo3AM();
					break;
				case 4:
					var today = Date.today();
					if (today.getDay() == 0) { // already the end of the billing week
						$scope.filter.dateRange.start.setDate(today.getDate() - 6);
						$scope.filter.dateRange.end.setDate(today.getDate() + 1);
					} else {
						while ($scope.filter.dateRange.start.getDay() > 1)
							$scope.filter.dateRange.start.setDate($scope.filter.dateRange.start.getDate() - 1);
						$scope.filter.dateRange.end.setDate($scope.filter.dateRange.start.getDate() + 7);
					}
					setTo3AM();
					break;
				case 5:
					var today = Date.today();
					if (today.getDay() == 0) { // already the end of the billing week
						$scope.filter.dateRange.start.setDate(today.getDate() - 13);
						$scope.filter.dateRange.end.setDate(today.getDate() - 6);
					} else {
						while ($scope.filter.dateRange.start.getDay() > 1)
							$scope.filter.dateRange.start.setDate($scope.filter.dateRange.start.getDate() - 1);
						$scope.filter.dateRange.start.setDate($scope.filter.dateRange.start.getDate() - 7); // previous week
						$scope.filter.dateRange.end.setDate($scope.filter.dateRange.start.getDate() + 7);
					}
					setTo3AM();
					break;
				default:
					$scope.filter.dateRange.start.setDate($scope.filter.dateRange.start.getDate() - 7);
					setTo3AM();
					break;
			}
			$scope.generateReport();
			$scope.tabs.index = 1;
		} else {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please wait until your first report is finished!")
					.position("top right")
					.hideDelay(3000)
			);
		}
	}

	if (globalReport.report == null || globalReport.report.length == 0) {
		globalReport = { report: null }; // place holder while report is genereated
		SitesService.listAll().then(function(response) {
			if (response.data.status_code == 0) {
				$scope.sites = response.data.report;
				if (mobileAndTabletcheck())
					$scope.query.limit = 25;
				else
					$scope.query.limit = $scope.sites.length;
				for (var i = 0; i < $scope.sites.length; i++) {
					$scope.sites[i].moneyHold = NaN;
					$scope.sites[i].moneyPercent = NaN; // fixes UI glitch for mobile;
					$scope.sites[i].siteLastPing = convertUTCDateToLocalDate(Date.parse($scope.sites[i].siteLastPing));
				}
				globalReport.report = angular.extend([], response.data.report);
			} else {
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
			}
			setTableHeight();
		});
		$scope.reportGenerated = false;
	} else {
		$scope.sites = angular.extend([], globalReport.report);
		if (globalReport.dateRange != null && globalReport.dateRange.start != null)
			$scope.filter.dateRange = angular.extend({}, globalReport.dateRange);
		$scope.reportGenerated = false;
		angular.forEach($scope.sites, function(value, key) {
			if (value.moneyIn != undefined || value.moneyOut != undefined) {
				$scope.reportGenerated = true;
				return;
			}
		});
	}

	// Returns formatted start and end time
	function getTimes() {
		if ($scope.timeSupported) {
			if (isNaN($scope.filter.dateRange.startTime.getHours()))
				return ["03:00:00", "03:00:00"];
			return [($scope.filter.dateRange.startTime.getHours() < 10 ? "0" + $scope.filter.dateRange.startTime.getHours() : $scope.filter.dateRange.startTime.getHours()) + ":" +
				($scope.filter.dateRange.startTime.getMinutes() < 10 ? "0" + $scope.filter.dateRange.startTime.getMinutes() : $scope.filter.dateRange.startTime.getMinutes()) + ":" +
				($scope.filter.dateRange.startTime.getSeconds() < 10 ? "0" + $scope.filter.dateRange.startTime.getSeconds() : $scope.filter.dateRange.startTime.getSeconds()),
			($scope.filter.dateRange.endTime.getHours() < 10 ? "0" + $scope.filter.dateRange.endTime.getHours() : $scope.filter.dateRange.endTime.getHours()) + ":" +
			($scope.filter.dateRange.endTime.getMinutes() < 10 ? "0" + $scope.filter.dateRange.endTime.getMinutes() : $scope.filter.dateRange.endTime.getMinutes()) + ":" +
			($scope.filter.dateRange.endTime.getSeconds() < 10 ? "0" + $scope.filter.dateRange.endTime.getSeconds() : $scope.filter.dateRange.endTime.getSeconds())];
		} else {
			var pattern = /(\d|\d\d).(\d\d)([A|a|P|p].)/;
			var startTime = "03:00:00", endTime = "03:00:00";
			var _start = pattern.exec($('#start-time').val()),
				_end = pattern.exec($('#end-time').val());
			if (_start) {
				if (_start[3].toLowerCase() == 'am' && _start[1] == '12')
					_start[1] = 0;
				if (_start[3].toLowerCase() == 'pm')
					_start[1] = parseInt(_start[1]) + 12;
				startTime = ("00" + _start[1]).substr(-2, 2) + ":" + _start[2] + ":00";
			}
			if (_end) {
				if (_end[3].toLowerCase() == 'am' && _end[1] == '12')
					_end[1] = 0;
				if (_end[3].toLowerCase() == 'pm')
					_end[1] = parseInt(_end[1]) + 12;
				endTime = ("00" + _end[1]).substr(-2, 2) + ":" + _end[2] + ":00";
			}
			return [startTime, endTime];
		}
	}

	$scope.printReport = function() {
		var times = getTimes();
		var startTime = times[0], endTime = times[1];
		document.location = "/print?start=" + $scope.filter.dateRange.start.yyyymmdd() + "T" + startTime + "&end=" + $scope.filter.dateRange.end.yyyymmdd() + "T" + endTime
			+ ($scope.filter.selectedSystem ? "&system=" + $scope.filter.selectedSystem : "") + "&sort=" + $scope.query.order;
	}
	$scope.downloadReport = function() {
		var blob = $scope.filter.dateRange.start.toString("MM-dd-yyyy") + " to " + $scope.filter.dateRange.end.toString("MM-dd-yyyy") + "\n"
			+ "ID,Distributor,System,Name,In,Out,Hold,Percent\n";
		var copy = angular.extend([], $scope.sites);

		var reverse = false;
		var order = $scope.query.order;
		if (order.charAt(0) == '-') {
			reverse = true;
			order = order.substring(1);
		}

		if (order == 'siteNumber')
			copy.sort(function(x, y) {
				if (x.siteNumber < y.siteNumber) return -1;
				if (x.siteNumber > y.siteNumber) return 1;
				return 0;
			});
		else if (order == 'distributor')
			copy.sort(function(x, y) {
				if (x.distributor < y.distributor) return -1;
				if (x.distributor > y.distributor) return 1;
				return 0;
			});
		else if (order == 'systemName')
			copy.sort(function(x, y) {
				if (x.systemName < y.systemName) return -1;
				if (x.systemName > y.systemName) return 1;
				return 0;
			});
		else if (order == 'siteName')
			copy.sort(function(x, y) {
				if (x.siteName < y.siteName) return -1;
				if (x.siteName > y.siteName) return 1;
				return 0;
			});
		else if (order == 'moneyIn')
			copy.sort(function(x, y) {
				if (x.moneyIn < y.moneyIn) return -1;
				if (x.moneyIn > y.moneyIn) return 1;
				return 0;
			});
		else if (order == 'moneyOut')
			copy.sort(function(x, y) {
				if (x.moneyOut < y.moneyOut) return -1;
				if (x.moneyOut > y.moneyOut) return 1;
				return 0;
			});
		else if (order == 'moneyHold')
			copy.sort(function(x, y) {
				if (x.moneyHold < y.moneyHold) return -1;
				if (x.moneyHold > y.moneyHold) return 1;
				return 0;
			});
		else if (order == 'moneyPercent')
			copy.sort(function(x, y) {
				if (x.moneyPercent < y.moneyPercent) return -1;
				if (x.moneyPercent > y.moneyPercent) return 1;
				return 0;
			});

		if (reverse) copy.reverse();
		for (var i = 0; i < copy.length; i++)
			blob += copy[i].siteNumber + ",\"" + copy[i].distributor.trim() + "\"," + copy[i].systemName + ",\"" + copy[i].siteName + "\"," +
				"\"" + $filter('currency')(copy[i].moneyIn, '$') + "\",\"" + $filter('currency')(copy[i].moneyOut, '$') + "\"," +
				"\"" + $filter('currency')(copy[i].moneyHold, '$') + "\",\"" + $filter('number')((copy[i].moneyPercent || 0) * 100, 2) + "%\"\n";

		saveAs(
			new Blob([blob], { type: "text/plain;charset=" + document.characterSet }),
			"report.csv"
		);
	}

	// Set limit per page to 25 so it's less intensive for mobile devices
	if (mobileAndTabletcheck())
		$scope.query.limit = 25;

	// Generates a report within the specified period of time (time range stored in $scope.filter.dateRange)
	$scope.generateReport = function() {
		if ($scope.canGenerateReport) {
			if (globalReport.report == null) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Please wait! Fetching site list...")
						.position("top right")
						.hideDelay(3000)
				);
				return;
			}
			for (var i = 0; i < globalReport.report.length; i++) {
				globalReport.report[i].moneyIn = null;
				globalReport.report[i].moneyOut = null;
				globalReport.report[i].moneyHold = null;
				globalReport.report[i].moneyPercent = 0; // fixed UI glitch for mobile; null makes it weird
			}

			$scope.canGenerateReport = false;
			var times = getTimes();
			var startTime = times[0], endTime = times[1];
			SitesService.reportList($scope.filter.dateRange.start.yyyymmdd() + " " + (startTime || "03:00:00"), $scope.filter.dateRange.end.yyyymmdd() + " " + (endTime || "03:00:00")).then(function(response) {
				if (mobileAndTabletcheck())
					$scope.query.limit = 25;
				else
					$scope.query.limit = response.data.report.length;
				if (response.data.status_code == 0) {
					var report = response.data.report;
					for (var i = 0; i < response.data.report.length; i++) {
						for (var k = 0; k < globalReport.report.length; k++) {
							if (response.data.report[i].siteId == globalReport.report[k].id) {
								var percent = (report[i].moneyIn - report[i].moneyOut) / report[i].moneyIn
								globalReport.report[k].moneyIn = report[i].moneyIn;
								globalReport.report[k].moneyOut = report[i].moneyOut;
								globalReport.report[k].moneyHold = report[i].moneyIn - report[i].moneyOut;
								globalReport.report[k].moneyPercent = isNaN(percent) ? 0 : percent;
								$scope.sites = angular.extend([], globalReport.report);
							}
						}
					}
					globalReport.dateRange = angular.extend({}, $scope.filter.dateRange);
					$scope.updateResultsPerPage();
					$scope.canGenerateReport = true;
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
				setTableHeight();
				$scope.reportGenerated = true;
				if (window.innerWidth < 1024)
					$('html, body').animate({
						scrollTop: $('#filterSystemSelect').offset().top
					}, 2000);

				$scope.systemFilterChanged();
			});
		}
	}

	function setTableHeight() {
		var tableHeight = 56; // 56 = height of table header row
		var tableWrapper = document.getElementById('tableWrapper');
		var tableBody = document.getElementById('tableBody');
		if (tableWrapper == null || tableBody == null) return; // page hasn't loaded yet

		for (var i = 0; i < tableBody.children.length; i++)
			tableHeight += tableBody.children[i].offsetHeight;

		tableWrapper.style.minHeight = tableHeight + 'px';
	}
	
	setInterval(function() {
		setTableHeight();
	}, 500);

	// Used for OnChange event for changing results per page
	$scope.updateResultsPerPage = function() {
		delete $scope.pages;
		$scope.pages = [];
		var totalPages = Math.ceil($scope.sites.length / $scope.resultsPerPage);
		for (var i = 0; i < totalPages; i++)
			$scope.pages[i] = i;

		if ($scope.pageIndex > totalPages - 1)
			$scope.pageIndex = totalPages - 1;
	}

	// Sidenav functions (open/close)
	$scope.toggleLeft = function() {
		$mdSidenav('left').toggle();
	}
	$scope.toggleRight = function() {
		$mdSidenav('right').toggle();
	}

	$scope.logout = function() {
		$window.localStorage.setItem('token', '');
		$window.location.href = "/login?status=1";
	};

	// SPA navigation
	$scope.changeSPA = function(page, ev) {
		switch (page) {
			case 1:
				$scope.toggleLeft();
				$mdDialog.show({
					controller: AddUserCtrl,
					templateUrl: 'tmpl/add-user.tmpl.html',
					parent: angular.element(document.body),
					targetEvent: ev,
					clickOutsideToClose: true
				}).then(function(dialogResponse) {
					if (dialogResponse == null) return;
					UserService.create(dialogResponse.user).then(function(response) {
						if (response.data.status_code == 0) {
							$mdToast.show(
								$mdToast.simple()
									.textContent("Successfully created new user!")
									.position("top right")
									.hideDelay(3000)
							);
							for (var i = 0; i < dialogResponse.sites.length; i++) {
								if (dialogResponse.sites[i].user_has_access)
									PermissionsService.setPermission(response.data.id, dialogResponse.sites[i].id, true).then(function(response) {
										if (response.data.status_code != 0)
											$mdToast.show(
												$mdToast.simple()
													.textContent("Unable to assign user to site '" + dialogResponse.sites[i].name + "'")
													.position("top right")
													.hideDelay(3000)
											);
									});
							}
							setTimeout(function() {
								if (document.location.pathname == "/edit-user")
									document.location.reload();
							}, 2000);
						} else {
							$mdToast.show(
								$mdToast.simple()
									.textContent("[" + response.data.status_code + "] " + response.data.status)
									.position("top right")
									.hideDelay(3000)
							);
						}
					});
				});
				break;
			case 2:
				$scope.toggleLeft();
				$location.path('/edit-user').search({});
				break;
			case 3:
				$scope.toggleLeft();
				break;
			case 4:
				$scope.toggleRight();
				$location.path('/account').search({});
				break;
			case 5:
				$scope.toggleLeft();
				$location.path('/activate').search({});
				break;
			case 6:
				$scope.toggleLeft();
				$location.path('/disable').search({});
				break;
			case 7:
				$scope.toggleLeft();
				$location.path('/logs').search({});
				break;
			case 8:
				$scope.toggleLeft();
				$location.path('/adjustments').search({});
				break;
			case 9:
				$scope.toggleLeft();
				$location.path('/players').search({});
				break;
			case 10:
				$scope.toggleLeft();
				$location.path('/tickets').search({});
				break;
			default:
				$scope.toggleLeft();
				$location.path('/').search({});
				break;
		}
	}
});
// Bulk adjustment controller
// url: /
function BulkAdjustmentCtrl($scope, $window, $mdDialog, $mdToast,
	AdjustmentService, UserInfo) {

	$scope.adjustment = { restartDate: Date.today(), restartTime: Date.parse("20:00:00") };

	AdjustmentService.getAdjustmentTypes().then(function(response) {
		if (response.data.status_code == 0)
			$scope.adjustmentTypes = response.data.report;
		else
			$mdDialog.hide();

		$scope.close = function() {
			$mdDialog.hide();
		}

		$scope.adjustment = { restartDate: Date.today().addDays(1), restartTime: Date.parse("06:00:00") };
		$scope.adjustTypeChanged = function() {
			if ($scope.adjustment.type > 5) {
				$scope.disableReset = true;
				$scope.adjustment.resetRequest = false;
			} else {
				$scope.disableReset = false;
			}
		}
		$scope.submitAdjustment = function() {
			if ($scope.adjustment.type == null) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Please select an adjustment type")
						.position("top right")
						.hideDelay(3000)
				);
				return;
			}
			if ($scope.adjustment.notes == '' || $scope.adjustment.notes == null) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Please enter notes")
						.position("top right")
						.hideDelay(3000)
				);
				return;
			}

			var formattedBody = {
				'Notes': $scope.adjustment.notes,
				'ResetRequest': $scope.adjustment.resetRequest,
				'DropGrandPrize': $scope.adjustment.grandPrize,
				'DropCommunityPrize': $scope.adjustment.communityPrize,
				'Type': $scope.adjustment.type,
				'RestartTime': $scope.adjustment.restartDate.toString("yyyy-MM-dd") + "T" + $scope.adjustment.restartTime.toString("HH:mm:ss") + ".000Z"
			};

			$mdToast.show(
				$mdToast.simple()
					.textContent("Submitting & verifying adjustment, please wait...")
					.position("top right")
					.hideDelay(3000)
			);

			$mdDialog.hide($scope.adjustment);
		}
		$scope.deleteAdjustment = function(adjustment, ev) {
			$mdDialog.show(
				$mdDialog.confirm()
					.title("Are you sure?")
					.textContent("This will permanently delete this adjustment")
					.ariaLabel("Delete adjustment")
					.targetEvent(ev)
					.ok('Yes')
					.cancel('No')
			).then(function() {
				AdjustmentService.deleteAdjustment(adjustment.id).then(function(response) {
					if (response.data.status_code == 0) {
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully deleted adjustment")
								.position("top right")
								.hideDelay(3000)
						);
						for (var i = 0; i < $scope.adjustments.length; i++)
							if ($scope.adjustments[i].id == adjustment.id)
								$scope.adjustments.splice(i, 1);
					}
				});
			});
		}
		$scope.grandPrizeChanged = function() {
			if ($scope.adjustment.grandPrize) {
				$scope.adjustment.communityPrize = false;
				$scope.adjustment.resetRequest = false;
				$scope.adjustment.type = -1;
				$scope.adjustment.size = -1;
			}
		};
		$scope.communityPrizeChanged = function() {
			if ($scope.adjustment.communityPrize) {
				$scope.adjustment.grandPrize = false;
				$scope.adjustment.resetRequest = false;
				$scope.adjustment.type = -1;
				$scope.adjustment.size = -1;
			}
		};
	});
}
// Bulk distributor set
// url: /
function BulkSetDistribCtrl($scope, $window, $mdDialog, $mdToast,
	UserService, UserInfo) {
	$scope.site = { distribId: 0 };

	UserService.distributors().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.distributors = response.data.report;
			$scope.site.distribId = $scope.distributors[0].id;
		} else {
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
			$mdDialog.hide();
		}
	});

	$scope.close = function() {
		$mdDialog.hide();
	}
	$scope.submit = function() {
		if ($scope.site.distribId != 0) {
			for (var i = 0; i < $scope.distributors.length; i++)
				if ($scope.distributors[i].id == $scope.site.distribId) {
					$mdDialog.hide($scope.distributors[i]);
					return;
				}
		}
	}
}
// Notifications page controller
// url: /notifications
app.controller('NotificationCtrl', function($scope, $window, $location,
	ActivationService, AdjustmentService, PlayerAdjustmentService, UserInfo) {
	UserInfo.updateToken();

	$scope.filter = {
		notificationType: null,
		yourNotifications: false,
		adjustments: false,
		activations: false
	};

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/notifications";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.authUser = response.data.subject;
	});

	$scope.notifications = [];

	$scope.notificationClicked = function(n) {
		switch (n.notificationType) {
			case 0:
				// approval
				window.location = "/site/" + n.siteId + "?tab=1";
				break;
			case 1:
				// server adjustment
				window.location = "/site/" + n.siteId + "?tab=2";
				break;
			case 2:
				// player pin adjustment
				window.location = "/site/" + n.siteId + "?tab=3";
				break;
			default:
				break;
		}
	}

	$scope.onlyActivations = function() {
		if ($scope.filter.activations) {
			$scope.filter.adjustments = false;
			$scope.filter.notificationType = 0;
		}
		if (!$scope.filter.adjustments && !$scope.filter.activations)
			$scope.filter.notificationType = null;
	}
	$scope.onlyAdjustments = function() {
		if ($scope.filter.adjustments) {
			$scope.filter.activations = false;
			$scope.filter.notificationType = 1;
		}
		if (!$scope.filter.adjustments && !$scope.filter.activations)
			$scope.filter.notificationType = null;
	}

	$scope.getNotificationTitle = function(n) {
		if (n.notificationType == 0)
			return "Unapproved Activation";
		else if (n.notificationType == 1)
			return "Processing Server Adjustment";
		else
			return "Pending Player Pin Adjustment";
	}
	$scope.getNotificationDescription = function(n) {
		if (n.notificationType == 0)
			return "Site '" + n.roomName + "' needs approval";
		else if (n.notificationType == 1)
			return "A server adjustment is being processed";
		else
			return "A player pin adjustment needs to be completed";
	}

	ActivationService.getUnapproved().then(function(response) {
		if (response.data.status_code == 0)
			angular.forEach(response.data.report, function(activation) {
				activation.notificationType = 0;
				$scope.notifications.push(activation);
			});
	});
	AdjustmentService.getUnfinished().then(function(response) {
		if (response.data.status_code == 0)
			angular.forEach(response.data.report, function(adjustment) {
				adjustment.notificationType = 1;
				$scope.notifications.push(adjustment);
			});
	});
	PlayerAdjustmentService.getUnclaimedAdjustments().then(function(response) {
		if (response.data.status_code == 0)
			angular.forEach(response.data.report, function(adjustment) {
				adjustment.notificationType = 2;
				$scope.notifications.push(adjustment);
			});
	});
});
// Add User Dialog
// Can be viewed on any page via sidenav
function AddUserCtrl($scope, $window, $mdDialog, $mdToast,
	AuthService, SitesService, UserInfo, UserService) {
	UserInfo.updateToken(); // Updates user access token
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/notifications";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	$scope.user = { 'level': 1 };
	$scope.hide = function() { $mdDialog.hide() }
	$scope.cancel = $scope.hide;
	$scope.create = function() {
		switch (verifyFields()) {
			case 0:
				$mdDialog.hide({ 'user': $scope.user, 'sites': $scope.sites });
				break;
			case 1:
				$scope.errorMessage = "Please give a first and last name.";
				break;
			case 2:
				$scope.errorMessage = "Please give a username.";
				break;
			case 3:
				$scope.errorMessage = "Please create a password.";
				break;
			case 4:
				$scope.errorMessage = "Passwords do not match.";
				break;
			case 5:
				$scope.errorMessage = "Please give either an email or a phone number.";
				break;
		}
	}
	$scope.createDistrib = function(ans) {
		$scope.user.level = 3;
		$scope.create();
	}
	// Permission levels
	UserService.levels().then(function(response) {
		if (response.data.status_code == 0)
			$scope.levels = response.data.report;
	});

	// Generate list of all sites to change which sites user has access to
	SitesService.listAll().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.sites = response.data.report;
			for (var i = 0; i < $scope.sites.length; i++)
				$scope.sites[i].user_has_access = false;
		} else {
			$mdDialog.hide();
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		}
	});

	// Checks to make sure all required fields are filled
	// 0 = success, 1 = no first and last name, 2 = no username, 3 = no password, 4 = password mismatch, 5 = no phone or email (only 1 required)
	function verifyFields() {
		if ($scope.user == null || $scope.user.fName == "" || $scope.user.fName == null || $scope.user.lName == "" || $scope.user.lName == null)
			return 1;
		else if ($scope.user.userName == "" || $scope.user.userName == null)
			return 2;
		else if ($scope.user.password == "" || $scope.user.password == null)
			return 3;
		else if ($scope.user.password != $scope.user.confirm_password)
			return 4;
		else if (($scope.user.phone == "" || $scope.user.phone == null) && ($scope.user.email == "" || $scope.user.email == null))
			return 5;
		return 0;
	}

	// "Add" and "Remove" buttons
	$scope.addSite = function() {
		var siteSelection = document.getElementById('multi_select1');
		if (siteSelection == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("An unexpected error occured. Please try again later.")
					.position("top right")
					.hideDelay(3000)
			);
			$mdDialog.hide();
		}

		var options = siteSelection.options
		for (var i = 0; i < options.length; i++) {
			var opt = options[i];
			if (opt.selected) {
				var id = (opt.value || opt.text).match(/[\d]+/)[0];
				for (var k = 0; k < $scope.sites.length; k++)
					if ($scope.sites[k].siteNumber == id)
						$scope.sites[k].user_has_access = true;
			}
		}
	}
	$scope.removeSite = function() {
		var siteSelection = document.getElementById('multi_select2');
		if (siteSelection == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("An unexpected error occured. Please try again later.")
					.position("top right")
					.hideDelay(3000)
			);
			$mdDialog.hide();
		}

		var options = siteSelection.options
		for (var i = 0; i < options.length; i++) {
			var opt = options[i];
			if (opt.selected) {
				var id = (opt.value || opt.text).match(/[\d]+/)[0];
				for (var k = 0; k < $scope.sites.length; k++)
					if ($scope.sites[k].siteNumber == id)
						$scope.sites[k].user_has_access = false;
			}
		}
	}
}
// Site view/modifier controller
// url: /site/:siteId
app.controller('SiteViewCtrl', function($scope, $window, $location, $routeParams, $mdToast, $mdDialog,
	ActivationService, AdjustmentService, AuthService, PermissionsService, PlayerAdjustmentService, PlayersService, SitesService, SystemService, UserService, UserInfo) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/site/" + $routeParams.siteId;
			return;
		}
		$scope.authUserId = response.data.subject;
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	$scope.tabs = {
		index: $routeParams.tab != null ? parseInt($routeParams.tab, 10) : 0
	}

	// Use this to check if prize drop should be shown 
	//(default value is 1970-01-01, but I've seen someone set database value to 1960-01-01, so using this just to be safe)
	var cutoffDate = Date.parse("2000-01-01");

	// Copies for checking what was modified
	var originalDistrib = {}, originalSite = {};
	$scope.siteDistributor = 1;
	$scope.system = 0;

	function canCreateUser() {
		if ($scope.permissionLevel == null) {
			$scope.canCreateUser = false;
			return;
		}
		if ($scope.addUserLevel == null) {
			$scope.canCreateUser = true;
			return;
		}

		for (var i = 0; i < $scope.addUserLevel.length; i++)
			if ($scope.addUserLevel[i] == $scope.permissionLevel)
				$scope.canCreateUser = true;
	}
	function canEditUser() {
		if ($scope.permissionLevel == null) {
			$scope.canEditUser = false;
			return;
		}
		if ($scope.modifyUserLevel == null) {
			$scope.canEditUser = true;
			return;
		}

		for (var i = 0; i < $scope.modifyUserLevel.length; i++)
			if ($scope.modifyUserLevel[i] == $scope.permissionLevel)
				$scope.canEditUser = true;
	}
	function canDeleteSite() {
		if ($scope.permissionLevel == null) {
			$scope.canDeleteSite = false;
			return;
		}
		if ($scope.deleteSiteLevel == null) {
			$scope.canDeleteSite = true;
			return;
		}

		for (var i = 0; i < $scope.modifySiteLevel.length; i++)
			if ($scope.deleteSiteLevel[i] == $scope.permissionLevel)
				$scope.canDeleteSite = true;
	}
	function canEditSite() {
		if ($scope.permissionLevel == null) {
			$scope.canEditSite = false;
			return;
		}
		if ($scope.modifySiteLevel == null) {
			$scope.canEditSite = true;
			return;
		}

		for (var i = 0; i < $scope.modifySiteLevel.length; i++)
			if ($scope.modifySiteLevel[i] == $scope.permissionLevel)
				$scope.canEditSite = true;
	}
	function canAddSite() {
		if ($scope.permissionLevel == null || $scope.adjustmentLevel == null) {
			$scope.canAddSite = false;
			return;
		}

		for (var i = 0; i < $scope.addSiteLevel.length; i++)
			if ($scope.addSiteLevel[i] == $scope.permissionLevel)
				$scope.canAddSite = true;
	}
	function canAdjust() {
		if ($scope.permissionLevel == null || $scope.adjustmentLevel == null) {
			$scope.canDoAdjustments = false;
			return;
		}
		// will never be open for all to do, so no need to check
		for (var i = 0; i < $scope.adjustmentLevel.length; i++)
			if ($scope.adjustmentLevel[i] == $scope.permissionLevel)
				$scope.canDoAdjustments = true;
	}
	function updatePermissions() {
		// Calling these all as individual functions because:
		//	1) 2 http requests are called asynchronously, so no guarantee which one finishes first (#getLinks() & #getAuthorization)
		//	2) Each function verifies that both async functions have been called
		// honestly these could be combined into one but I'm too lazy tbh
		canCreateUser();
		canEditUser();
		canEditSite();
		canAdjust();
		canAddSite();
		canDeleteSite();
	}
	PermissionsService.getLinks().then(function(response) {
		if (response.data.status_code == 0) { // plot twist: it's always 0
			$scope.addUserLevel = response.data.addUser;
			$scope.addSiteLevel = response.data.addSite;
			$scope.modifyUserLevel = response.data.modifyUser;
			$scope.modifySiteLevel = response.data.modifySite;
			$scope.adjustmentLevel = response.data.adjustments;
			$scope.deleteSiteLevel = response.data.deleteSite;
			$scope.sidenavLinks = response.data.links;
			updatePermissions();
		}
	});
	AuthService.getAuthorization().then(function(response) {
		$scope.permissionLevel = response.data.level;
		$scope.user = { id: response.data.subject, name: response.data.name };
		$scope.userId = response.data.subject;
		updatePermissions();
	});

	$scope.close = function() {
		$location.path('/').search({});
	}
	$scope.reactivate = function(id) {
		$location.path('/reactivate/' + $scope.lastForm).search({});
	}
	$scope.approve = function(form) {
		if (form.approvalNotes == null || form.approvalNotes == "") {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please enter your approval notes")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		ActivationService.approve(form).then(function(response) {
			if (response.data.status_code == 0) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Successfully approved site")
						.position("top right")
						.hideDelay(3000)
				);
				$location.path("/").search({});
			} else
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
		});
	}
	$scope.preApprove = function() {
		window.sessionStorage.setItem('pre-approved', $routeParams.siteId);
		$location.path('/preapprove').search({});
	}
	$scope.deleteActivation = function(form, ev) {
		var confirm = $mdDialog.confirm()
			.title("Are you sure you want to delete this form?")
			.textContent("You cannot undo this action")
			.ariaLabel("Delete activation form")
			.targetEvent(ev)
			.ok("Yes")
			.cancel("No");

		$mdDialog.show(confirm).then(function() {
			ActivationService.deleteActivation(form.activationId).then(function(response) {
				if (response.data.status_code == 0) {
					for (var i = 0; i < $scope.activations.length; i++)
						if ($scope.activations[i].activation.activationId == form.activationId)
							$scope.activations.splice(i, 1);
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully deleted form")
							.position("top right")
							.hideDelay(3000)
					);
					if ($scope.activations.length == 0)
						$scope.success = -1;
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
			});
		});
	}

	// used to check if getting activation forms has any errors
	$scope.success = -1;

	// Generates list for distributor select menu & get activation forms
	UserService.distributors().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.distributors = response.data.report;

			// Get activation form for site
			ActivationService.getActivation($routeParams.siteId).then(function(response) {
				if (response.data.status_code == 0) {
					$scope.activations = response.data.report;
					$scope.lastForm = response.data.report[0].activation.activationId;
					$scope.success = 1;
					angular.forEach($scope.activations, function(form) {
						form.activation.installDate = Date.parse(form.activation.installDate);
						form.activation.submissionDate = Date.parse(form.activation.submissionDate);
						for (var i = 0; i < $scope.distributors.length; i++) {
							if ($scope.distributors[i].id == form.activation.distributorId)
								form.activation.distributorUserName = $scope.distributors[i].userName;
						}
					});
					$scope.latestActivation = $scope.activations[0];
				} else {
					$scope.success = -1;
				}
			});
		} else {
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		}
	});

	// Generates all other info in both tabs
	SitesService.site($routeParams.siteId).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.site = response.data.site;
			$scope.system = response.data.system;
			$scope.activation = response.data.activation;
			$scope.site.siteDateInstalled = $scope.site.siteDateInstalled != null ? new Date($scope.site.siteDateInstalled) : new Date("1970-01-01 03:00:00");
			$scope.site.storeOpenTime = Date.parse($scope.site.storeOpenTime);
			$scope.site.storeCloseTime = Date.parse($scope.site.storeCloseTime);
			$scope.site.lastCommunityDrop = Date.parse($scope.site.lastCommunityDrop);
			$scope.site.lastGrandDrop = Date.parse($scope.site.lastGrandDrop);
			originalSite = angular.extend({}, response.data.site);

			$scope.communityDropValid = $scope.site.lastCommunityDrop > cutoffDate;
			$scope.grandDropValid = $scope.site.lastGrandDrop > cutoffDate;

			UserService.user($scope.site.siteDistributor).then(function(response) {
				$scope.distributor = response.data.user;
				originalDistrib = angular.extend({}, response.data.user);
			});
			// Generates list for system select menu
			SystemService.list().then(function(response) {
				if (response.data.status_code == 0) {
					// tech/admin
					$scope.systems = response.data.report;
				} else {
					// normal user
					$scope.systems = null;
					$scope.system = "Loading...";
					SystemService.system($scope.site.systemId).then(function(response) {
						if (response.data.status_code == 0)
							$scope.system = response.data.system;
						else
							$scope.system = "[Internal Error - please report this]";
					});
				}
			});
		} else {
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		}
	});

	// Save site info
	$scope.saveInfo = function() {
		$scope.storeOpenTime = $scope.storeOpenTime ? Date.parse($scope.storeOpenTime).toString("HH:mm:ss a") : "08:00:00";
		$scope.storeCloseTime = $scope.storeCloseTime ? Date.parse($scope.storeCloseTime).toString("HH:mm:ss a") : "20:00:00";
		$scope.isTwentyFourSeven = $scope.isTwentyFourSeven || false;

		var siteKeys = Object.keys(originalSite), distribKeys = Object.keys(originalDistrib);
		var siteChanged = false, siteChangedDistribs = false, distribChanged = false;
		for (var i = 0; i < siteKeys.length; i++) {
			var key = siteKeys[i];
			if ($scope.site[key] != originalSite[key]) {
				siteChanged = true;
				if (key == "siteDistributor")
					siteChangedDistribs = true;
				break;
			}
		}
		for (var i = 0; i < distribKeys.length; i++) {
			var key = distribKeys[i];
			if ($scope.distributor[key] != originalDistrib[key]) {
				distribChanged = true;
				break;
			}
		}

		// Only true if distributor info is changed
		if (siteChangedDistribs) {
			delete $scope.distributor;
			$scope.distributor = { 'fName': 'Loading new info...', 'lName': 'Please wait.' };
			UserService.user($scope.site.siteDistributor).then(function(response) {
				$scope.distributor = response.data.user;
				originalDistrib = angular.extend({}, response.data.user);
			});
		}
		// Only true if site info is updated
		if (siteChanged) {
			var formattedSite = angular.extend({}, $scope.site);
			formattedSite.siteId = $routeParams.siteId;
			formattedSite.siteDateInstalled = formattedSite.siteDateInstalled.toJSON();
			formattedSite.storeOpenTime = $scope.site.storeOpenTime.toString("HH:mm:ss");
			formattedSite.storeCloseTime = $scope.site.storeCloseTime.toString("HH:mm:ss");
			formattedSite.isTwentyFourSeven = $scope.site.isTwentyFourSeven || false;
			SitesService.update(formattedSite).then(function(response) {
				if (response.data.status_code == 0) {
					if (globalReport != null && globalReport.report != null) {
						for (var i = 0; i < globalReport.report.length; i++) {
							if (globalReport.report[i].id == response.data.site.siteId) {
								globalReport.report[i] = angular.extend(globalReport.report[i], response.data.site);
							}
							if (globalReport.report[i].siteActive == false)
								globalReport.report.splice(i, 1);
							else
								// Instead of just pushing value to site, it would involve either:
								//  a) making a new route and/or modifying current route to get more information than needed just for this specific case
								//  b) making a couple HTTP GET requests just to get the distributor name and system name again
								// so the easiest and most efficient way (and also a way to potentially avoid future issues) is populate the site list again.
								// This also makes more sense because it will clear their currently generated report (if there is one) so it forces them to generate
								// another report, which they would have to do again no matter what if they wanted to see the money in/out for this newly added site
								SitesService.listAll().then(function(response) {
									if (response.data.status_code == 0) {
										for (var i = 0; i < $scope.sites.length; i++)
											$scope.sites[i].moneyPercent = 0; // fixes UI glitch for mobile;
										globalReport.report = angular.extend([], response.data.report);
									} else {
										$mdToast.show(
											$mdToast.simple()
												.textContent("[" + response.data.status_code + "] " + response.data.status)
												.position("top right")
												.hideDelay(3000)
										);
									}
								});
						}
					}
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully updated site!")
							.position("top right")
							.hideDelay(3000)
					);
					$scope.site = response.data.site;
				} else
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
			});
			originalSite = angular.extend({}, $scope.site);
			siteChanged = false;
		}
		// Only true if distributor for site is changed; updates distributor tab
		if (distribChanged) {
			if ($scope.distributor.percentage && $scope.distributor.percentage >= 0 && $scope.distributor.percentage <= 100) {
				UserService.update($scope.distributor).then(function(response) {
					if (response.data.status_code == 0)
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully updated distributor info!")
								.position("top right")
								.hideDelay(3000)
						);
					else
						$mdToast.show(
							$mdToast.simple()
								.textContent("[" + response.data.status_code + "] " + response.data.status)
								.position("top right")
								.hideDelay(3000)
						);
				});
				originalDistrib = angular.extend({}, $scope.distributor);
				distribChanged = false;
			} else {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Percentage must be between 0-100")
						.position("top right")
						.hideDelay(3000)
				);
				distribChanged = false;
			}
		}

		if (siteChanged || siteChangedDistribs)
			$location.path('/');
	}

	/// ** Adjustments ** ///
	$scope.adjustment = { restartDate: Date.today().addDays(1), restartTime: Date.parse("06:00:00") };
	$scope.getAdjustmentType = function(type) {
		if (type >= 0 && type <= 2)
			return 'green';
		else if (type >= 3 && type <= 5)
			return 'red';
		else
			return 'blue';
	}
	$scope.getAdjustmentColor = function(percent, mobile) {
		var targetRange = 0.3; // 30% is in the green
		var acceptance = 0.2; // +/- 20% is still good

		if (percent <= 0.5 && percent >= 0.2)
			return mobile ? 'green-300' : 'green';
		else if (percent < 0.1 || percent > 0.7)
			return mobile ? 'red-300' : 'red';
		else
			return mobile ? 'amber-200' : 'amber';
	}
	$scope.getAdjustmentText = function(adjustment) {
		// Returning array with 2 indices so I don't need 2 separate functions (ex: getAdjustmentFirstWord() and getAdjustmentSecondWord())
		switch (adjustment.type) {
			case 0:
				return ['Small', 'Increase'];
			case 1:
				return ['Medium', 'Increase'];
			case 2:
				return ['Large', 'Increase'];
			case 3:
				return ['Small', 'Decrease'];
			case 4:
				return ['Medium', 'Decrease'];
			case 5:
				return ['Large', 'Decrease'];
			case 6:
				return ['Full', 'Reset'];
			case 7:
				return ['Grand', 'Prize Drop'];
			case 8:
				return ['Community', 'Prize Drop'];
			default:
				return ['Unknown', 'Type'];
		}
	}
	$scope.adjustTypeChanged = function() {
		if ($scope.adjustment.type > 5) {
			$scope.disableReset = true;
			$scope.adjustment.resetRequest = false;
		} else {
			$scope.disableReset = false;
		}
	}
	$scope.submitAdjustment = function() {
		if ($scope.adjustment.type == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please select an adjustment type")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		if ($scope.adjustment.notes == '' || $scope.adjustment.notes == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please enter notes")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}

		var formattedBody = {
			'Notes': $scope.adjustment.notes,
			'ResetRequest': $scope.adjustment.resetRequest,
			'SiteId': $routeParams.siteId,
			'DropGrandPrize': $scope.adjustment.grandPrize,
			'DropCommunityPrize': $scope.adjustment.communityPrize,
			'Type': $scope.adjustment.type,
			'RestartTime': $scope.adjustment.restartDate.toString("yyyy-MM-dd") + "T" + $scope.adjustment.restartTime.toString("HH:mm:ss") + ".000Z"
		};

		$scope.adjustment = { restartDate: Date.today(), restartTime: Date.parse("20:00:00") };
		$mdToast.show(
			$mdToast.simple()
				.textContent("Submitting & verifying adjustment, please wait...")
				.position("top right")
				.hideDelay(3000)
		);

		AdjustmentService.submit(formattedBody).then(function(response) {
			if (response.data.status_code == 0) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Successfully submitted adjustment")
						.position("top right")
						.hideDelay(3000)
				);
				$scope.adjustment = { restartDate: Date.today(), restartTime: Date.parse("20:00:00") };

				AdjustmentService.getAdjustments($routeParams.siteId).then(function(response) {
					if (response.data.status_code == 0) {
						$scope.adjustments = response.data.report;
						angular.forEach($scope.adjustments, function(adjustment) {
							adjustment.submissionDate = Date.parse(adjustment.submissionDate);
							adjustment.weekHold = adjustment.weekMoneyIn - adjustment.weekMoneyOut;
							adjustment.weekPercent = adjustment.weekHold / adjustment.weekMoneyIn;
							adjustment.monthHold = adjustment.monthMoneyIn - adjustment.monthMoneyOut;
							adjustment.monthPercent = adjustment.monthHold / adjustment.monthMoneyIn;
						});
					} else {
						$window.location.href = "/";
					}
				});
			} else {
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
			}
		});
	}
	$scope.deleteAdjustment = function(adjustment, ev) {
		$mdDialog.show(
			$mdDialog.confirm()
				.title("Are you sure?")
				.textContent("This will permanently delete this adjustment")
				.ariaLabel("Delete adjustment")
				.targetEvent(ev)
				.ok('Yes')
				.cancel('No')
		).then(function() {
			AdjustmentService.deleteAdjustment(adjustment.id).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully deleted adjustment")
							.position("top right")
							.hideDelay(3000)
					);
					for (var i = 0; i < $scope.adjustments.length; i++)
						if ($scope.adjustments[i].id == adjustment.id)
							$scope.adjustments.splice(i, 1);
				}
			});
		});
	}
	$scope.statusButtonClicked = function(adjustment, ev) {
		if (!adjustment.completed)
			$mdDialog.show(
				$mdDialog.alert()
					.clickOutsideToClose(true)
					.title('Server Adjustment Notes')
					.textContent("Notes: " + adjustment.notes + (adjustment.restartTime ? " | Restart time: " + Date.parse(adjustment.restartTime).toString("MM/dd/yy hh:mm tt") : ""))
					.ariaLabel('ServerNotes')
					.ok('Close')
					.targetEvent(ev)
			);
		else
			$mdDialog.show({
				locals: { adjustment: adjustment },
				controller: AdjustmentCompleteCtrl,
				templateUrl: 'tmpl/adjustment-complete.tmpl.html',
				parent: angular.element(document.body),
				targetEvent: ev,
				clickOutsideToClose: true
			});
	}
	$scope.grandPrizeChanged = function() {
		if ($scope.adjustment.grandPrize) {
			$scope.adjustment.communityPrize = false;
			$scope.adjustment.resetRequest = false;
			$scope.adjustment.type = -1;
			$scope.adjustment.size = -1;
		}
	};
	$scope.communityPrizeChanged = function() {
		if ($scope.adjustment.communityPrize) {
			$scope.adjustment.grandPrize = false;
			$scope.adjustment.resetRequest = false;
			$scope.adjustment.type = -1;
			$scope.adjustment.size = -1;
		}
	};
	AdjustmentService.getAdjustments($routeParams.siteId).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.adjustments = response.data.report;
			angular.forEach($scope.adjustments, function(adjustment) {
				adjustment.submissionDate = convertUTCDateToLocalDate(Date.parse(adjustment.submissionDate));
				adjustment.claimedDated   = convertUTCDateToLocalDate(Date.parse(adjustment.claimedDate));
				adjustment.completedDated = convertUTCDateToLocalDate(Date.parse(adjustment.completedDate));
				adjustment.weekHold       = adjustment.weekMoneyIn  - adjustment.weekMoneyOut;
				adjustment.weekPercent    = adjustment.weekHold  / adjustment.weekMoneyIn;
				adjustment.monthHold      = adjustment.monthMoneyIn - adjustment.monthMoneyOut;
				adjustment.monthPercent   = adjustment.monthHold / adjustment.monthMoneyIn;
			});
		}
	});
	AdjustmentService.getAdjustmentTypes().then(function(response) {
		if (response.data.status_code == 0)
			$scope.adjustmentTypes = response.data.report;
	});

	/// ** Player pins ** ///
	$scope.today = Date.today();
	$scope.request = { restartTime: null };
	$scope.getPlayerPinDisabled = function(pin) {
		if (pin.claimedBy == null)
			return false;
		else if (pin.completedTime != null)
			return true;
		else if (pin.completedTime == null && pin.claimedBy != $scope.userId)
			return true;
		return false;
	}
	$scope.getPlayerPinText = function(pin) {
		if (pin.claimedBy == 0 || pin.claimedBy == null)
			return "Claim";
		else if (pin.completedTime == null)
			return (pin.claimedBy == $scope.user.id ? "Complete" : "Claimed");
		return "Completed";
	};
	$scope.playerPinClicked = function(pin, ev, encoded) {
		// Claim adjustment
		if (pin.claimedBy == 0 || pin.claimedBy == null) {
			$mdDialog.show(
				$mdDialog.confirm()
					.title("Are you sure?")
					.textContent("Once you claim an adjustment, you cannot unclaim it and only you or an admin can delete it")
					.ariaLabel("Confirmation")
					.ok("Yes")
					.cancel("No")
					.targetEvent(ev)
			).then(function(response) {
				PlayerAdjustmentService.claim(pin.id).then(function(response) {
					if (response.data.status_code == 0) {
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully claimed adjustment")
								.position("top right")
								.hideDelay(3000)
						);
						pin.claimedBy = $scope.user.id;
						pin.claimedByName = $scope.user.name;
						$scope.playerPinClicked(pin, ev, response.data.encoded); // automatically show dialog to complete adjustment
					} else {
						$mdToast.show(
							$mdToast.simple()
								.textContent("[" + response.data.status_code + "] " + response.data.status)
								.position("top right")
								.hideDelay(3000)
						);
					}
				});
			});
		} else if (pin.claimedBy == $scope.user.id) {
			// Coming back to already claimed adjustment
			if (encoded == null) {
				PlayerAdjustmentService.getAdjustment(pin.id).then(function(response) {
					$mdDialog.show({
						controller: CompletePlayerPinCtrl,
						templateUrl: 'tmpl/adjustment-claim.tmpl.html',
						parent: angular.element(document.body),
						locals: { adjustment: pin, code: response.data.encoded },
						targetEvent: ev,
						clickOutsideToClose: true
					}).then(function(response) {
						if (response === true) {
							pin.completedTime = Date.now();
							$mdToast.show(
								$mdToast.simple()
									.textContent("Successfully completed adjustment")
									.position("top right")
									.hideDelay(3000)
							);
						}
					});
				});
				
			} else {
				$mdDialog.show({
					controller: CompletePlayerPinCtrl,
					templateUrl: 'tmpl/adjustment-claim.tmpl.html',
					parent: angular.element(document.body),
					locals: { adjustment: pin, code: encoded },
					targetEvent: ev,
					clickOutsideToClose: true
				}).then(function(response) {
					if (response === true) {
						pin.completedTime = Date.now();
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully completed adjustment")
								.position("top right")
								.hideDelay(3000)
						);
					}
				});
			}
		}
	};
	$scope.deletePin = function(pin, ev) {
		var confirm = $mdDialog.confirm()
			.title('Delete player pin adjustment?')
			.textContent('Are you sure you want to do this?\nYou cannot undo this deleteion.')
			.ariaLabel('PinDeletion')
			.clickOutsideToClose(true)
			.targetEvent(ev)
			.ok('Yes')
			.cancel('No');

		$mdDialog.show(confirm).then(function(response) {
			PlayerAdjustmentService.delete(pin).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully deleted player pin adjustment")
							.position("top right")
							.hideDelay(3000)
					);
					angular.forEach($scope.playerPins, function(value, index) {
						if (value.id == pin.id) {
							$scope.playerPins.splice(index, 1);
							return;
						}
					});
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
			})
		})
	};
	$scope.submitPlayerAdjustment = function() {
		if ($scope.request.amount == null || $scope.request.amount < 0) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please enter a valid amount")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		if ($scope.request.cardNumber == null || $scope.request.cardNumber == "") {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please enter a valid card number")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		if ($scope.request.side == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please select adjustment type")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		if ($scope.request.notes == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Please enter notes")
					.position("top right")
					.hideDelay(3000)
			);
			return;
		}
		
		PlayerAdjustmentService.create({
			'Amount': $scope.request.amount * 100, // `Amount` is stored in cents on server, so need to convert dollar => cent
			'SiteId': $routeParams.siteId,
			'CardNumber': $scope.request.cardNumber,
			'Type': $scope.request.side,
			'Notes': $scope.request.notes,
			'RestartTime': null
		}).then(function(response) {
			if (response.data.status_code == 0) {
				$mdToast.show(
					$mdToast.simple()
						.textContent("Successfully submitted player pin adjustment")
						.position("top right")
						.hideDelay(3000)
				);
				if ($scope.request.side == 9)
					response.data.adjustment.typeName = "Playable";
				else
					response.data.adjustment.typeName = "Cashable";
				response.data.adjustment.amount = response.data.adjustment.amount / 100;

				$scope.request = {};
				$scope.playerPins.push(response.data.adjustment);
			}
		});
	}
	PlayerAdjustmentService.getSiteAdjustments($routeParams.siteId).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.playerPins = response.data.report;
			angular.forEach(response.data.report, function(pinAdjustment) {
				pinAdjustment.amount = pinAdjustment.amount / 100;
			});
		}
	});

	/// ** Player balances ** ///
	// Filter options
	$scope.query = {
		order: 'cardId',
		limit: 50,
		page: 1
	}
	$scope.filter = {};
	$scope.limitOptions = [25, 50, 75, 100, {
		label: 'All',
		value: function() {
			return $scope.users ? $scope.users.length : 0;
		}
	}];

	$scope.playerBalanceClicked = function(player, ev) {
		$mdDialog.show({
			controller: PlayerInfoCtrl,
			templateUrl: 'tmpl/player-info.tmpl.html',
			locals: { player: player },
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		});
	};

	PlayersService.listAll($routeParams.siteId).then(function(response) {
		if (response.data.status_code == 0) {
			angular.forEach(response.data.report, function(player) {
				player.cashableBalance = player.cashableBalance ? player.cashableBalance / 100 : 0;
				player.playableBalance = player.playableBalance ? player.playableBalance / 100 : 0;
				player.birthday = convertUTCDateToLocalDate(Date.parse(player.birthday));
				player.name = player.firstName + " " + player.lastName;
			});
			$scope.players = response.data.report;
		}
	});
});
function CompletePlayerPinCtrl($scope, $window, $mdToast, $mdDialog, adjustment, code,
	PlayerAdjustmentService, UserInfo) {
	$scope.adjustment = adjustment;
	adjustment.code = code;

	$scope.close = function() {
		$mdDialog.hide();
	}
	$scope.submitAdjustment = function() {
		PlayerAdjustmentService.complete(adjustment.id, adjustment.verification).then(function(response) {
			if (response.data.status_code == 0)
				$mdDialog.hide(true);
			else
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
		});
	}
}
function PlayerInfoCtrl($scope, $window, $mdDialog, player, UserInfo) {
	$scope.player = player;

	$scope.close = function() {
		$mdDialog.hide();
	}
}
// User Modification Controller
// url: /edit-user
app.controller('EditUserCtrl', function($scope, $window, $location,
	AuthService, UserInfo, UserService) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/edit-user";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	$scope.query = {
		order: 'id',
		limit: 99999,
		page: 1
	}
	$scope.filter = {};
	$scope.limitOptions = [25, 50, 75, 100, {
		label: 'All',
		value: function() {
			return $scope.users ? $scope.users.length : 0;
		}
	}];

	// Fixes bug: when on page 2+ and searching, if search result doesn't generate 2+ pages of search results,
	// no results will be rendered
	$scope.searchQueryChanged = function() {
		if ($scope.filter.searchText.trim() != "")
			$scope.query.page = 1;
	}

	$scope.previous = function() {
		if (($scope.query.page - 1) > 0)
			$scope.query.page = $scope.query.page - 1;
	}
	$scope.next = function() {
		if (($scope.query.page) * $scope.query.limit < $scope.users.length)
			$scope.query.page = $scope.query.page + 1;
	}

	// Fixes bug: when on page 2+ and searching, if search result doesn't generate 2+ pages of search results,
	// no results will be rendered
	$scope.searchQueryChanged = function() {
		if ($scope.filter.searchText.trim() != "")
			$scope.query.page = 1;
	}

	UserService.users().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.users = response.data.users;
			for (var i = 0; i < $scope.users.length; i++)
				$scope.users[i].name = ($scope.users[i].fName || "") + " " + ($scope.users[i].lName || "");
		}
	});
});
// User view/modifier controller
// url: /edit-user/:userId
app.controller('UserViewCtrl', function($scope, $window, $location, $routeParams, $mdDialog, $mdToast,
	AuthService, PermissionsService, SitesService, UserInfo, UserService) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/edit-user/" + $routeParams.userId;
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	// Backup copy of data to compare to see if user made any changes
	var originalUser = {}, originalSites = [];

	// Get user permission level to see if they can modify page
	$scope.canEditUser = false;

	PermissionsService.getLinks().then(function(response) {
		if (response.data.status_code == 0) { // plot twist: it's always 0
			$scope.addUserLevel = response.data.addUser;
			$scope.modifyUserLevel = response.data.modifyUser;
			$scope.addSiteLevel = response.data.addSite;
			$scope.sidenavLinks = response.data.links;
			updatePermissions();
		}
	});

	AuthService.getAuthorization().then(function(response) {
		$scope.permissionLevel = response.data.level;
		updatePermissions();
	});

	function canEditUser() {
		if ($scope.permissionLevel == null) {
			$scope.canEditUser = false;
			return;
		}
		if ($scope.modifyUserLevel == null) {
			$scope.canEditUser = true;
			return;
		}

		for (var i = 0; i < $scope.modifyUserLevel.length; i++)
			if ($scope.modifyUserLevel[i] == $scope.permissionLevel)
				$scope.canEditUser = true;
	}
	function canCreateSite() {
		if ($scope.permissionLevel == null) {
			$scope.canCreateSite = false;
			return;
		}
		if ($scope.addSiteLevel == null) {
			$scope.canCreateSite = true;
			return;
		}

		for (var i = 0; i < $scope.addSiteLevel.length; i++)
			if ($scope.addSiteLevel[i] == $scope.permissionLevel)
				$scope.canCreateSite = true;
	}
	function updatePermissions() {
		canEditUser();
		canCreateSite();
	}

	// Generate level permission list
	UserService.levels().then(function(response) {
		if (response.data.status_code == 0)
			$scope.levels = response.data.report;
	});

	// Until `is_distributor` field is removed from database, keep this code to adjust checkbox
	var changedValue;
	$scope.isDistributorTicked = function() {
		changedValue = $scope.user.isDistributor;
	}
	$scope.levelChanged = function() {
		if ($scope.user.level == 3)
			$scope.user.isDistributor = true;
		else
			$scope.user.isDistributor = (changedValue != null ? changedValue : (originalUser.isDistributor || false));
	}

	// Get user info
	UserService.user($routeParams.userId).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.user = response.data.user;
			$scope.user.userLastLogin = $scope.user.userLastLogin ? new Date($scope.user.userLastLogin) : null;
			originalUser = angular.extend({}, $scope.user);
		} else {
			console.log("[" + response.data.status_code + "] " + response.data.status);
		}
	});

	// Determines whether user can make changes to page or not
	AuthService.getAuthorization().then(function(response) {
		$scope.authUser = response.data;
		if ($scope.authUser.role == "10" || $scope.authUser.id == $routeParams.userId)
			$scope.canEditUser = true;
	});

	// Generate list of all sites to change which sites user has access to
	SitesService.listAll().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.sites = response.data.report;
			for (var i = 0; i < $scope.sites.length; i++) {
				$scope.sites[i].user_has_access = false;
			}

			// Sets `user_has_access` to true for all sites user has access to
			PermissionsService.getPermission($routeParams.userId).then(function(response) {
				if (response.data.status_code == 0) {
					for (var i = 0; i < $scope.sites.length; i++) {
						for (var k = 0; k < response.data.report.length; k++) {
							if ($scope.sites[i].id == response.data.report[k])
								$scope.sites[i].user_has_access = true;
						}
						originalSites.push(angular.extend({}, $scope.sites[i]));
					}
				} // no need to do anything if status_code != 0 b/c can't do anything + user is probably fixing that issue (hopefully lol)
			});
		} else {
			$mdDialog.hide();
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		}
	});

	// "Add" and "Remove" buttons
	$scope.addSite = function() {
		var siteSelection = document.getElementById('multi_select1');
		if (siteSelection == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("An unexpected error occured. Please try again later.")
					.position("top right")
					.hideDelay(3000)
			);
			$mdDialog.hide();
		}

		var options = siteSelection.options
		for (var i = 0; i < options.length; i++) {
			var opt = options[i];
			if (opt.selected) {
				var id = (opt.value || opt.text).match(/[\d]+/)[0];
				for (var k = 0; k < $scope.sites.length; k++)
					if ($scope.sites[k].siteNumber == id)
						$scope.sites[k].user_has_access = true;
			}
		}
	}
	$scope.removeSite = function() {
		var siteSelection = document.getElementById('multi_select2');
		if (siteSelection == null) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("An unexpected error occured. Please try again later.")
					.position("top right")
					.hideDelay(3000)
			);
			$mdDialog.hide();
		}

		var options = siteSelection.options
		for (var i = 0; i < options.length; i++) {
			var opt = options[i];
			if (opt.selected) {
				var id = (opt.value || opt.text).match(/[\d]+/)[0];
				for (var k = 0; k < $scope.sites.length; k++)
					if ($scope.sites[k].siteNumber == id)
						$scope.sites[k].user_has_access = false;
			}
		}
	}

	$scope.changePwd = function(ev) {
		$mdDialog.show({
			controller: UpdatePwdCtrl,
			templateUrl: 'tmpl/change-pwd.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			response.id = $scope.user.id;
			UserService.update(response, true).then(function(httpResponse) {
				if (httpResponse.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully updated password!")
							.position("top right")
							.hideDelay(3000)
					);
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + httpResponse.data.status_code + "] " + httpResponse.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
			});
		});
	}

	$scope.deleteUser = function(ev) {
		// Appending dialog to document.body to cover sidenav in docs app
		var confirm = $mdDialog.confirm()
			.title('Are you sure you want to delete this user?')
			.textContent('There is no way to undo this action.')
			.ariaLabel('Delete user')
			.targetEvent(ev)
			.ok('Yes')
			.cancel('No');

		$mdDialog.show(confirm).then(function() {
			UserService.delete($scope.user.id).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully deleted '" + $scope.user.userName + "'!")
							.position("top right")
							.hideDelay(3000)
					);
					$location.path('/edit-user')
				} else
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + httpResponse.data.status_code + "] " + httpResponse.data.status)
							.position("top right")
							.hideDelay(3000)
					);
			});
		}, function() { });
	};

	$scope.saveInfo = function() {
		//if (GetLoggedInUserLevel() != 2 || 10) return
		var userInfoChanged = false, sitesChanged = false;
		var userKeys = Object.keys(originalUser);
		for (var i = 0; i < userKeys.length; i++) {
			var key = userKeys[i];
			if (originalUser[key] != $scope.user[key]) {
				userInfoChanged = true;
				break;
			}
		}

		if (userInfoChanged) {
			UserService.update($scope.user).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully updated user info!")
							.position("top right")
							.hideDelay(3000)
					);
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
			});
			$location.path('/edit-user');
		}

		for (var i = 0; i < $scope.sites.length; i++) {
			if ($scope.sites[i].user_has_access != originalSites[i].user_has_access) {
				PermissionsService.setPermission($scope.user.id, $scope.sites[i].id, $scope.sites[i].user_has_access).then(function(response) {
					if (response.data.status_code != 0)
						$mdToast.show(
							$mdToast.simple()
								.textContent("[" + response.data.status_code + "] " + response.data.status)
								.position("top right")
								.hideDelay(3000)
						);
				});
				sitesChanged = true;
			}
		}
		if (sitesChanged && !userInfoChanged)
			$mdToast.show(
				$mdToast.simple()
					.textContent("Successfully updated permissions!")
					.position("top right")
					.hideDelay(3000)
			);
	}
});
// User Account Controller
// url: /account
app.controller('AccountCtrl', function($scope, $window, $location, $mdDialog, $mdToast,
	AuthService, PermissionsService, UserInfo, UserService) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/account";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	AuthService.getAuthorization().then(function(response) {
		if (response.data.subject != null)
			UserService.user(response.data.subject).then(function(response) {
				if (response.data.status_code == 0)
					$scope.account = response.data.user;
				else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
				}
			});
		else
			$mdToast.show(
				$mdToast.simple()
					.textContent("An unexpected error occured, please try again")
					.position("top right")
					.hideDelay(3000)
			);
	});

	$scope.changePwd = function(ev) {
		$mdDialog.show({
			controller: UpdatePwdCtrl,
			templateUrl: 'tmpl/change-pwd.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			if ($scope.account == null) return;
			response.id = $scope.account.id;
			UserService.update(response, true).then(function(httpResponse) {
				if (httpResponse.data.status_code == 0)
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully updated password!")
							.position("top right")
							.hideDelay(3000)
					);
				else
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + httpResponse.data.status_code + "] " + httpResponse.data.status)
							.position("top right")
							.hideDelay(3000)
					);
			});
		});
	}
	$scope.saveInfo = function() {
		if ($scope.account == null) return;
		UserService.update($scope.account).then(function(response) {
			if (response.data.status_code == 0)
				$mdToast.show(
					$mdToast.simple()
						.textContent("Successfully updated account!")
						.position("top right")
						.hideDelay(3000)
				);
			else
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
		});
		$location.path('/');
	}
});
// Update Password Dialog controller
// url: /account from `Change Password` button
function UpdatePwdCtrl($scope, $window, $mdDialog,
	AuthService, UserInfo) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	$scope.user = {};
	$scope.hide = function() { $mdDialog.hide() }
	$scope.cancel = $scope.hide;
	$scope.update = function(ans) {
		switch (verifyFields()) {
			case 0:
				$mdDialog.hide($scope.user);
				break;
			case 1:
				$scope.errorMessage = "Please enter a password.";
				break;
			case 2:
				$scope.errorMessage = "Passwords do not match.";
				break;
		}
	}

	// Checks to make sure all required fields are filled
	// 0 = success, 1 = no first and last name, 2 = no username, 3 = no password, 4 = password mismatch, 5 = no phone or email (only 1 required)
	function verifyFields() {
		if ($scope.user.password == "" || $scope.user.password == null)
			return 1;
		else if ($scope.user.password != $scope.user.confirm_password)
			return 2;
		return 0;
	}
}
// Site activation controller
// url: /activate
app.controller('ActivateSiteCtrl', function($scope, $location, $window, $mdToast,
	ActivationService, InstallerService, ManagerService, PermissionsService, SystemService, UserInfo, UserService) {
	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/activate";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.permissionLevel = response.data.level;
	});

	$scope.room = { installer: 0, distributor: -1, manager: 0, ownerId: 0, key: Guid.create(), install_date: Date.today(), openTime: Date.parse("08:00 am"), closeTime: Date.parse("08:00 pm") };
	$scope.user = {}; $scope.manager = {};
	$scope.installer = {}; $scope.distributor = {};
	$scope.installers = []; $scope.managers = [];

	InstallerService.getInstallers().then(function(response) {
		if (response.data.status_code == 0)
			$scope.installers = response.data.report;
	});
	ManagerService.getManagers().then(function(response) {
		if (response.data.status_code == 0)
			$scope.managers = response.data.report;
	});

	$scope.today = Date.today();
	$scope.errorMessage = '';

	UserService.distributors().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.distributors = response.data.report;
			$scope.distributors.sort(function(x, y) {
				if (x.name.trim() < y.name.trim()) return -1;
				if (x.name.trim() > y.name.trim()) return 1;
				return 0;
			});
		}
	});
	SystemService.list().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.systems = response.data.report;
		}
	});
	UserService.owners().then(function(response) {
		if (response.data.stauts_code == 0) {
			$scope.owners = response.data.report;
			$scope.owners.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});

	$scope.installerChanged = function() {
		if ($scope.room.installer == 0)
			$scope.installer = {};
		else
			angular.forEach($scope.installers, function(value, key) {
				if (value.id == $scope.room.installer)
					$scope.installer = value
			});
	}
	$scope.distributorChanged = function() {
		if ($scope.room.distributor == 0)
			$scope.distributor = {};
		else
			angular.forEach($scope.distributors, function(value, key) {
				if (value.id == $scope.room.distributor)
					$scope.distributor = value;
			});
	}
	$scope.managerChanged = function() {
		if ($scope.room.manager == 0)
			$scope.manager = {};
		else
			angular.forEach($scope.managers, function(value, key) {
				if (value.id == $scope.room.manager)
					$scope.manager = value;
			});
	}
	$scope.ownerChanged = function() {
		if ($scope.room.ownerId == 0)
			$scope.owner = {};
		else
			angular.forEach($scope.owners, function(value, key) {
				if (value.id == $scope.room.ownerId) {
					$scope.owner = value;
				}
			});
	}

	$scope.cancel = function() {
		$location.path("/").search({});
	}
	$scope.fillWithData = function() {
		// pulled from randomlists.com
		var randomAddresses = [
			{ site_address: "309 William St", site_city: "Superior", site_state: "WI", site_zip: "54880" },
			{ site_address: "975 Virginia St", site_city: "Lake Mary", site_state: "FL", site_zip: "32746" },
			{ site_address: "319 Rock Creek Dr", site_city: "Evans", site_state: "GA", site_zip: "30809" },
			{ site_address: "77 Highland Road", site_city: "Dothan", site_state: "AL", site_zip: "36301" },
			{ site_address: "253 Lawrence Ln", site_city: "Lady Lake", site_state: "FL", site_zip: "32159" },
			{ site_address: "276 Gates Rd", site_city: "Deland", site_state: "FL", site_zip: "32720" },
			{ site_address: "901 Coffee St", site_city: "Andover", site_state: "MA", site_zip: "21810" },
			{ site_address: "9518 Carson Ave", site_city: "Westlake", site_state: "OH", site_zip: "44145" },
			{ site_address: "7484 Bow Ridge Ln", site_city: "Mason", site_state: "OH", site_zip: "45040" },
			{ site_address: "54 Stillwater Rd", site_city: "Marietta", site_state: "GA", site_zip: "30008" },
			{ site_address: "104 Peach St", site_city: "Atlanta", site_state: "GA", site_zip: "30253" }
		];

		// Alert user if no managers and/or installers exist
		if ($scope.installers.length == 0) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Warning: No installers exist; please make some up")
					.position("top right")
					.hideDelay(3000)
			); return;
		} else if ($scope.managers.length == 0) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Warning: No managers exist; please make one up")
					.position("top right")
					.hideDelay(3000)
			); return;
		} else if ($scope.owners.length == 0) {
			$mdToast.show(
				$mdToast.simple()
					.textContent("Warning: No owners exist; please make one up")
					.position("top right")
					.hideDelay(3000)
			); return;
		}

		// Randomly generate room data
		$scope.room = {
			name: "Generated Room #" + Math.floor(Math.random() * 1000),
			key: Guid.create(),
			system: $scope.systems[Math.floor(Math.random() * $scope.systems.length)].id,
			hp_id: "|GENERATED TEST ROOM|",
			install_date: Date.today(),
			email: "fake@email.com",
			phone: 7277776566,
			installer: $scope.installers.length > 0 ? $scope.installers[Math.floor(Math.random() * $scope.installers.length)].id : 0,
			manager: $scope.managers.length > 0 ? $scope.managers[Math.floor(Math.random() * $scope.managers.length)].id : 0,
			distributor: $scope.distributors[Math.floor(Math.random() * $scope.distributors.length)].id,
			ownerId: $scope.owners.length > 0 ? $scope.owners[Math.floor(Math.random() * $scope.owners.length)].id : 0,
			owner_name: "Mr Owner",
			owner_email: "owner@email.com",
			owner_phone: 7277776655,
			openTime: Date.parse("08:00 am"),
			closeTime: Date.parse("08:00 pm"),
			site_country: "USA",
			activation_notes: "|SITE GENERATED WITH FAKE DATA - THIS IS NOT A REAL SITE. DO NOT APPROVE|"
		}
		// pick from randomly generated address
		angular.extend($scope.room, randomAddresses[Math.floor(Math.random() * randomAddresses.length)]);
		// Randomly generated user login
		$scope.user = {
			userName: "FakeOwner" + Math.floor(Math.random() * 1000),
			password: "passwordZ!@#"
		}
		// Update fields
		$scope.installerChanged(); $scope.distributorChanged(); $scope.managerChanged(); $scope.ownerChanged();
	}
	$scope.submit = function() {
		if ($scope.room.name == null || $scope.room.name == '') {
			$scope.errorMessage = "Please enter a valid room name";
		} else if ($scope.room.hp_id == null || $scope.room.hp_id == '') {
			$scope.errorMessage = "Please enter valid server info";
		} else if ($scope.room.email == null || $scope.room.email == '' || $scope.room.phone == null || $scope.room.phone == '' || $scope.room.phone.length < 10) {
			$scope.errorMessage = "Please enter valid contact info";
		} else if ($scope.room.installer == 0 && ($scope.installer.fName == null || $scope.installer.fName == '' || $scope.installer.lName == null || $scope.installer.lName == '')) {
			$scope.errorMessage = "Please enter installer's first and last name";
		} else if ($scope.room.installer == 0 && ($scope.installer.phone == null || $scope.installer.phone == '')) {
			$scope.errorMessage = "Please enter installer's contact info (phone required, email optional)";
		} else if ($scope.room.manager == 0 && ($scope.manager.fName == null || $scope.manager.fName == '' || $scope.manager.lName == null || $scope.manager.lName == '')) {
			$scope.errorMessage = "Please enter manager's first and last name";
		} else if ($scope.room.manager == 0 && ($scope.manager.phone == null || $scope.manager.phone == '')) {
			$scope.errorMessage = "Please enter manager's contact info (phone required, email optional)";
		} else if ($scope.room.distributor == -1) {
			$scope.errorMessage = "Please select a distributor";
		} else if ($scope.room.distributor == 0 && ($scope.distributor.fName == null || $scope.distributor.fName == '' || $scope.distributor.lName == null || $scope.distributor.lName == '')) {
			$scope.errorMessage = "Please fill out new distributor first and last name";
		} else if ($scope.room.distributor == 0 && ($scope.distributor.userName == null || $scope.distributor.userName == '' || $scope.distributor.password == null || $scope.distributor.password == '')) {
			$scope.errorMessage = "Please fill out new distributor username and password";
		} else if ($scope.room.distributor == 0 && ($scope.distributor.phone == null || $scope.distributor.phone.length < 10)) {
			$scope.errorMessage = "Please fill out new distributor phone number";
		} else if ($scope.room.ownerId == 0 && ($scope.owner.fName == null || $scope.owner.fName == ""
			|| $scope.owner.lName == null || $scope.owner.lName == "" || $scope.owner.phone == null || $scope.owner.phone.length < 10)) {
			$scope.errorMessage = "Please enter valid owner information";
		} else if ($scope.room.site_address == null || $scope.room.site_address == '' || $scope.room.site_city == null || $scope.room.site_city == ''
			|| $scope.room.site_state == null || $scope.room.site_state == '' || $scope.room.site_country == null || $scope.room.site_country == ''
			|| $scope.room.site_zip == null || $scope.room.site_zip == '') {
			$scope.errorMessage = "Please enter valid location";
		} else if ($scope.room.ownerId == 0 && ($scope.owner.userName == null || $scope.owner.userName == '' || $scope.owner.password == null || $scope.owner.password == '')) {
			$scope.errorMessage = "Please enter a valid login";
		} else if ($scope.room.activation_notes == null || $scope.room.activation_notes == "") {
			$scope.errorMessage = "Please add information to activation notes";
		} else
			$scope.errorMessage = '';

		if ($scope.errorMessage != '') {
			$('#activation-form').animate({
				scrollTop: 0
			}, 500);
			return;
		}

		function _activate() {
			var name = $scope.owner.fName.trim() + " " + $scope.owner.lName.trim(); // Server grabs user info if `id` is present, so no need to set any other values
			var formattedRequest = {
				'NewSite': {
					'systemId': $scope.room.system,
					'siteGuid': $scope.room.key,
					'siteName': $scope.room.name,
					'siteDistributor': $scope.room.distributor,
					'siteAddress': $scope.room.site_address + ($scope.room.site_address2 ? " " + $scope.room.site_address2 : ""),
					'siteCity': $scope.room.site_city,
					'siteState': $scope.room.site_state,
					'siteZip': $scope.room.site_zip,
					'siteCountry': $scope.room.site_country,
					'siteOwnerName': $scope.room.ownerId == 0 ? $scope.owner.fName.trim() + " " + $scope.owner.lName.trim() : $scope.owner.name,
					'siteOwnerEmail': $scope.owner.email.trim() || '',
					'siteOwnerPhone': $scope.owner.phone || '',
					'storePhone': $scope.room.phone || '',
					'siteInstallDate': convertUTCDateToLocalDate($scope.room.install_date).toJSON(),
					'storeOpenTime': $scope.room.openTime.toString("HH:mm:ss"),
					'storeCloseTime': $scope.room.closeTime.toString("HH:mm:ss"),
					'isTwentyFourSeven': $scope.room.is247
				},
				'NewOwner': $scope.room.ownerId == 0 ? {
					'userName': $scope.owner.userName,
					'password': $scope.owner.password,
					'level': 2,
					'fName': $scope.owner.fName.trim(),
					'lName': $scope.owner.lName.trim(),
					'phone': $scope.owner.phone,
					'email': $scope.owner.email
				} : { 'id': $scope.room.ownerId }, // Server grabs user info if `id` is present, so no need to set any other values
				'SiteInstaller': angular.extend({}, $scope.installer),
				'SiteManager': angular.extend({}, $scope.manager),
				'ActivationInfo': {
					'hpId': $scope.room.hp_id,
					'key': $scope.room.key,
					'installDate': convertUTCDateToLocalDate($scope.room.install_date).toJSON(),
					'billingEmail': $scope.room.email,
					'activationNotes': $scope.room.activation_notes
				},
				'PreApproved': false
			}

			ActivationService.activate(formattedRequest).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully activated site, created user, and assigned to new site")
							.position("top right")
							.hideDelay(4000)
					);
					$location.path("/site/" + response.data.siteId).search({});
					globalReport.report = null;
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		}

		if ($scope.room.distributor == 0) {
			$scope.distributor.level = 3;
			$scope.distributor.isDistributor = true;
			UserService.create($scope.distributor).then(function(response) {
				if (response.data.status_code == 0) {
					$scope.room.distributor = response.data.id;
					_activate();
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		} else {
			_activate();
		}
	}

	function setWidth() {
		$scope.width = window.innerWidth;
	}
	setWidth();
	$(window).on('resize', setWidth);
});
// Re-Activation controller
// url: /reactivate/:formId
app.controller('ReactivateCtrl', function($scope, $location, $window, $mdToast, $routeParams,
	ActivationService, InstallerService, ManagerService, PermissionsService, SystemService, UserInfo, UserService) {
	var defaults = { installerId: 0, distributorId: -1, managerId: 0, ownerId: 0, key: Guid.create() };
	$scope.user = {}; $scope.manager = {}; $scope.owner = {};
	$scope.installer = {}; $scope.distributor = {};
	$scope.installers = []; $scope.managers = []; $scope.owners = [];

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/reactivate/" + $routeParams.formId;
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.permissionLevel = response.data.level;
	});

	InstallerService.getInstallers().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.installers = response.data.report;
			$scope.installers.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	ManagerService.getManagers().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.managers = response.data.report;
			$scope.managers.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	SystemService.list().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.systems = response.data.report;
			$scope.systems.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	UserService.distributors().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.distributors = response.data.report;
			$scope.distributors.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	UserService.owners().then(function(response) {
		if (response.data.stauts_code == 0) {
			$scope.owners = response.data.report;
			$scope.owners.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});

	ActivationService.getForm($routeParams.formId).then(function(response) {
		if (response.data.status_code == 0) {
			$scope.room = angular.extend(response.data.activation, defaults);
			$scope.room.siteInstallDate = Date.parse($scope.room.installDate);
		} else if (response.data.status_code == 8)
			$location.path('/site/' + $routeParams.formId).search({ tab: 2 });
	});

	$scope.today = Date.today();
	$scope.errorMessage = '';

	$scope.distributorChanged = function() {
		if ($scope.room.distributorId == 0)
			$scope.distributor = {};
		else
			angular.forEach($scope.distributors, function(value, key) {
				if (value.id == $scope.room.distributorId)
					$scope.distributor = value;
			});
	}
	$scope.installerChanged = function() {
		if ($scope.room.installerId == 0)
			$scope.installer = {};
		else
			angular.forEach($scope.installers, function(value, key) {
				if (value.id == $scope.room.installerId)
					$scope.installer = value
			});
	}
	$scope.managerChanged = function() {
		if ($scope.room.managerId == 0)
			$scope.manager = {};
		else
			angular.forEach($scope.managers, function(value, key) {
				if (value.id == $scope.room.managerId)
					$scope.manager = value;
			});
	}
	$scope.ownerChanged = function() {
		if ($scope.room.ownerId == 0)
			$scope.owner = {};
		else
			angular.forEach($scope.owners, function(value, key) {
				if (value.id == $scope.room.ownerId)
					$scope.owner = value;
			});
	}

	$scope.cancel = function() {
		$location.path("/").search({});
	}
	$scope.submit = function() {
		if ($scope.room.roomName == null || $scope.room.roomName == '') {
			$scope.errorMessage = "Please enter a valid room name";
		} else if ($scope.room.hpId == null || $scope.room.hpId == '') {
			$scope.errorMessage = "Please enter valid server info";
		} else if ($scope.room.billingEmail == null || $scope.room.billingEmail == '' || $scope.room.billingEmail.indexOf('@') == -1
			|| $scope.room.storePhone == null || $scope.room.storePhone == '' || $scope.room.storePhone.length < 10) {
			$scope.errorMessage = "Please enter valid contact info";
		} else if ($scope.room.siteInstallDate == null) {
			$scope.errorMessage = "Please enter a valid install date";
		} else if ($scope.room.installerId == 0 && ($scope.installer.fName == null || $scope.installer.fName == '' || $scope.installer.lName == null || $scope.installer.lName == '')) {
			$scope.errorMessage = "Please enter installer's first and last name";
		} else if ($scope.room.installerId == 0 && ($scope.installer.phone == null || $scope.installer.phone == '')) {
			$scope.errorMessage = "Please enter installer's contact info (phone required, email optional)";
		} else if ($scope.room.managerId == 0 && ($scope.manager.fName == null || $scope.manager.fName == '' || $scope.manager.lName == null || $scope.manager.lName == '')) {
			$scope.errorMessage = "Please enter manager's first and last name";
		} else if ($scope.room.managerId == 0 && ($scope.manager.phone == null || $scope.manager.phone == '')) {
			$scope.errorMessage = "Please enter manager's contact info (phone required, email optional)";
		} else if ($scope.room.distributorId == -1) {
			$scope.errorMessage = "Please select a distributor";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.fName == null || $scope.distributor.fName == '' || $scope.distributor.lName == null || $scope.distributor.lName == '')) {
			$scope.errorMessage = "Please fill out new distributor first and last name";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.userName == null || $scope.distributor.userName == '' || $scope.distributor.password == null || $scope.distributor.password == '')) {
			$scope.errorMessage = "Please fill out new distributor username and password";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.phone == null || $scope.distributor.phone.length < 10)) {
			$scope.errorMessage = "Please fill out new distributor phone number";
		} else if ($scope.room.ownerId == 0 && ($scope.owner.fName == null || $scope.owner.fName == '' || $scope.owner.lName == null || $scope.owner.lName == '')) {
			$scope.errorMessage = "Please fill out new owner first and last name";
		} else if ($scope.ownerId == 0 && ($scope.owner.phone == null || $scope.owner.phone.length < 10)) {
			$scope.errorMessage = "Please fill out new owner phone number";
		} else if ($scope.room.site_address == null || $scope.room.site_address == '' || $scope.room.siteCity == null || $scope.room.siteCity == ''
			|| $scope.room.siteState == null || $scope.room.siteState == '' || $scope.room.siteCountry == null || $scope.room.siteCountry == ''
			|| $scope.room.siteZip == null || $scope.room.siteZip == '') {
			$scope.errorMessage = "Please enter valid location";
		} else if ($scope.room.ownerId == 0 && ($scope.owner.userName == null || $scope.owner.userName == '' || $scope.owner.password == null || $scope.owner.password == '')) {
			$scope.errorMessage = "Please enter a valid login";
		} else
			$scope.errorMessage = '';

		if ($scope.errorMessage != '') {
			$('#activation-form').animate({
				scrollTop: 0
			}, 500);
			return;
		}

		function _activate() {
			var formattedRequest = {
				'NewSite': {
					'siteId': $scope.room.siteId,
					'systemId': $scope.room.systemId,
					'siteGuid': $scope.room.key,
					'siteName': $scope.room.roomName,
					'siteDistributor': $scope.room.distributorId,
					'siteAddress': $scope.room.site_address + ($scope.room.site_address2 ? " " + $scope.room.site_address2 : ""),
					'siteCity': $scope.room.siteCity,
					'siteState': $scope.room.siteState,
					'siteZip': $scope.room.siteZip,
					'siteCountry': $scope.room.siteCountry,
					'siteOwnerName': $scope.room.ownerId == 0 ? $scope.owner.fName.trim() + " " + $scope.owner.lName.trim() : $scope.owner.fName + " " + $scope.owner.lName,
					'siteOwnerEmail': $scope.room.ownerId == 0 ? ($scope.owner.email.trim() || '') : $scope.owner.email,
					'siteOwnerPhone': $scope.room.ownerId == 0 ? ($scope.owner.phone || '') : $scope.owner.phone,
					'storePhone': $scope.room.storePhone || '',
					'siteInstallDate': convertUTCDateToLocalDate($scope.room.siteInstallDate).toJSON()
				},
				'NewOwner': $scope.room.ownerId == 0 ? {
					'userName': $scope.owner.userName,
					'password': $scope.owner.password,
					'level': 2,
					'fName': $scope.owner.fName.trim(),
					'lName': $scope.owner.lName.trim(),
					'phone': $scope.owner.phone,
					'email': $scope.owner.email,
				} : { 'id': $scope.room.ownerId },
				'SiteInstaller': angular.extend({}, $scope.installer),
				'SiteManager': angular.extend({}, $scope.manager),
				'ActivationInfo': {
					'hpId': $scope.room.hpId,
					'key': $scope.room.key,
					'installDate': convertUTCDateToLocalDate($scope.room.siteInstallDate).toJSON(),
					'billingEmail': $scope.room.billingEmail,
					'activationNotes': $scope.room.reactivationNotes
				}
			}
			
			ActivationService.reactivate(formattedRequest).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully activated site, created user, and assigned to new site")
							.position("top right")
							.hideDelay(4000)
					);
					$location.path("/site/" + $scope.room.siteId).search({});
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		}

		if ($scope.room.distributorId == 0) {
			$scope.distributor.level = 3;
			$scope.distributor.isDistributor = true;
			UserService.create($scope.distributor).then(function(response) {
				if (response.data.status_code == 0) {
					$scope.room.distributor = response.data.id;
					_activate();
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		} else {
			_activate();
		}
	}

	function setWidth() {
		$scope.width = window.innerWidth;
	}
	setWidth();
	$(window).on('resize', setWidth);
});
// Pre-Approved activation controller
// url: /preapprove
app.controller('PreapprovedCtrl', function($scope, $location, $window, $mdToast, $routeParams,
	ActivationService, InstallerService, ManagerService, PermissionsService, SitesService, SystemService, UserInfo, UserService) {

	// Checks if site is pre-approved
	var check = window.sessionStorage.getItem("pre-approved");
	if (check) {
		$scope.preapprove = true;
		window.sessionStorage.removeItem("pre-approved");
		SitesService.site(check).then(function(response) {
			if (response.data.status_code == 0) {
				var site = response.data.site;
				$scope.room.roomName = site.siteName;
				$scope.room.siteNumber = site.siteNumber;
				$scope.room = angular.extend($scope.room, site);
				$scope.room.distributorId = site.siteDistributor;
				$scope.room.site_address = site.siteAddress;
				$scope.room.siteInstallDate = Date.parse($scope.room.siteInstallDate);
				$scope.room.siteLastPing = Date.parse($scope.room.siteLastPing);
				$scope.room.is247 = site.isTwentyFourSeven;
				$scope.room.openTime = site.storeOpenTime ? Date.parse(site.storeOpenTime) : Date.parse("08:00:00");
				$scope.room.closeTime = site.storeCloseTime ? Date.parse(site.storeCloseTime) : Date.parse("20:00:00");
				$scope.room.reactivationNotes = "This site was pre-approved through old report portal.";

				$scope.distributorChanged();
			} else {
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(3000)
				);
				$location.path("/").search({});
			}
		});
	} else {
		$location.path("/").search({});
	}

	$scope.room = { installerId: 0, distributorId: -1, managerId: 0, ownerId: 0, key: Guid.create() };
	$scope.user = {}; $scope.manager = {}; $scope.owner = {};
	$scope.installer = {}; $scope.distributor = {};
	$scope.installers = []; $scope.managers = []; $scope.owners = [];

	InstallerService.getInstallers().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.installers = response.data.report;
			$scope.installers.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	ManagerService.getManagers().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.managers = response.data.report;
			$scope.managers.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	SystemService.list().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.systems = response.data.report;
			$scope.systems.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});
	UserService.distributors().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.distributors = response.data.report;
			$scope.distributors.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
			$scope.distributorChanged();
		}
	});
	UserService.owners().then(function(response) {
		if (response.data.stauts_code == 0) {
			$scope.owners = response.data.report;
			$scope.owners.sort(function(x, y) {
				if (x.name.toLowerCase().trim() < y.name.toLowerCase().trim()) return -1;
				if (x.name.toLowerCase().trim() > y.name.toLowerCase().trim()) return 1;
				return 0;
			});
		}
	});

	$scope.today = Date.today();
	$scope.errorMessage = '';

	$scope.distributorChanged = function() {
		if ($scope.room.distributorId == 0)
			$scope.distributor = {};
		else
			angular.forEach($scope.distributors, function(value, key) {
				if (value.id == $scope.room.distributorId)
					$scope.distributor = value;
			});
	}
	$scope.installerChanged = function() {
		if ($scope.room.installerId == 0)
			$scope.installer = {};
		else
			angular.forEach($scope.installers, function(value, key) {
				if (value.id == $scope.room.installerId)
					$scope.installer = value
			});
	}
	$scope.managerChanged = function() {
		if ($scope.room.managerId == 0)
			$scope.manager = {};
		else
			angular.forEach($scope.managers, function(value, key) {
				if (value.id == $scope.room.managerId)
					$scope.manager = value;
			});
	}
	$scope.ownerChanged = function() {
		if ($scope.room.ownerId == 0)
			$scope.owner = {};
		else
			angular.forEach($scope.owners, function(value, key) {
				if (value.id == $scope.room.ownerId)
					$scope.owner = value;
			});
	}

	$scope.cancel = function() {
		$location.path("/").search({});
	}
	$scope.submit = function() {
		if ($scope.room.roomName == null || $scope.room.roomName == '') {
			$scope.errorMessage = "Please enter a valid room name";
		} else if ($scope.room.hpId == null || $scope.room.hpId == '') {
			$scope.errorMessage = "Please enter valid server info";
		} else if ($scope.room.billingEmail == null || $scope.room.billingEmail == '' || $scope.room.billingEmail.indexOf('@') == -1
			|| $scope.room.storePhone == null || $scope.room.storePhone == '' || $scope.room.storePhone.length < 10) {
			$scope.errorMessage = "Please enter valid contact info";
		} else if ($scope.room.siteInstallDate == null) {
			$scope.errorMessage = "Please enter a valid install date";
		} else if ($scope.room.installerId == 0 && ($scope.installer.fName == null || $scope.installer.fName == '' || $scope.installer.lName == null || $scope.installer.lName == '')) {
			$scope.errorMessage = "Please enter installer's first and last name";
		} else if ($scope.room.installerId == 0 && ($scope.installer.phone == null || $scope.installer.phone == '')) {
			$scope.errorMessage = "Please enter installer's contact info (phone required, email optional)";
		} else if ($scope.room.managerId == 0 && ($scope.manager.fName == null || $scope.manager.fName == '' || $scope.manager.lName == null || $scope.manager.lName == '')) {
			$scope.errorMessage = "Please enter manager's first and last name";
		} else if ($scope.room.managerId == 0 && ($scope.manager.phone == null || $scope.manager.phone == '')) {
			$scope.errorMessage = "Please enter manager's contact info (phone required, email optional)";
		} else if ($scope.room.distributorId == -1) {
			$scope.errorMessage = "Please select a distributor";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.fName == null || $scope.distributor.fName == '' || $scope.distributor.lName == null || $scope.distributor.lName == '')) {
			$scope.errorMessage = "Please fill out new distributor first and last name";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.userName == null || $scope.distributor.userName == '' || $scope.distributor.password == null || $scope.distributor.password == '')) {
			$scope.errorMessage = "Please fill out new distributor username and password";
		} else if ($scope.room.distributorId == 0 && ($scope.distributor.phone == null || $scope.distributor.phone.length < 10)) {
			$scope.errorMessage = "Please fill out new distributor phone number";
		} else if ($scope.room.site_address == null || $scope.room.site_address == '' || $scope.room.siteCity == null || $scope.room.siteCity == ''
			|| $scope.room.siteState == null || $scope.room.siteState == '' || $scope.room.siteCountry == null || $scope.room.siteCountry == ''
			|| $scope.room.siteZip == null || $scope.room.siteZip == '') {
			$scope.errorMessage = "Please enter valid location";
		} else
			$scope.errorMessage = '';

		if ($scope.errorMessage != '') {
			$('#activation-form').animate({
				scrollTop: 0
			}, 500);
			return;
		}

		function _activate() {
			var formattedRequest = {
				'NewSite': {
					'siteId': $scope.room.siteId,
					'siteNumber': $scope.room.siteNumber,
					'systemId': $scope.room.systemId,
					'siteGuid': $scope.room.key.value,
					'siteName': $scope.room.roomName,
					'siteDistributor': $scope.room.distributorId,
					'siteAddress': $scope.room.site_address + ($scope.room.site_address2 ? " " + $scope.room.site_address2 : ""),
					'siteCity': $scope.room.siteCity,
					'siteState': $scope.room.siteState,
					'siteZip': $scope.room.siteZip,
					'siteCountry': $scope.room.siteCountry,
					'siteOwnerName': $scope.owner.fName.trim() + " " + $scope.owner.lName.trim(),
					'siteOwnerEmail': $scope.owner.email.trim() || '',
					'siteOwnerPhone': $scope.owner.phone || '',
					'storePhone': $scope.room.storePhone || '',
					'siteInstallDate': convertUTCDateToLocalDate($scope.room.siteInstallDate).toJSON(),
					'storeOpenTime': $scope.room.openTime.toString("HH:mm:ss"),
					'storeCloseTime': $scope.room.closeTime.toString("HH:mm:ss"),
					'isTwentyFourSeven': $scope.room.is247
				},
				'NewOwner': $scope.room.ownerId == 0 ? {
					'userName': $scope.owner.userName,
					'password': $scope.owner.password,
					'level': 2,
					'fName': $scope.owner.fName.trim(),
					'lName': $scope.owner.lName.trim(),
					'phone': $scope.owner.phone,
					'email': $scope.owner.email,
				} : { 'id': $scope.room.ownerId },
				'SiteInstaller': angular.extend({}, $scope.installer),
				'SiteManager': angular.extend({}, $scope.manager),
				'ActivationInfo': {
					'hpId': $scope.room.hpId,
					'key': $scope.room.key.value,
					'installDate': convertUTCDateToLocalDate($scope.room.siteInstallDate).toJSON(),
					'billingEmail': $scope.room.billingEmail,
					'activationNotes': $scope.room.reactivationNotes
				},
				'PreApproved': true
			}

			ActivationService.reactivate(formattedRequest).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent("Successfully activated site, created user, and assigned to new site")
							.position("top right")
							.hideDelay(4000)
					);
					$location.path("/site/" + $scope.room.siteId).search({});
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		}

		if ($scope.room.distributorId == 0) {
			$scope.distributor.level = 3;
			$scope.distributor.isDistributor = true;
			UserService.create($scope.distributor).then(function(response) {
				if (response.data.status_code == 0) {
					$scope.room.distributor = response.data.id;
					_activate();
				} else {
					$mdToast.show(
						$mdToast.simple()
							.textContent("[" + response.data.status_code + "] " + response.data.status)
							.position("top right")
							.hideDelay(3000)
					);
					$scope.errorMessage = "[" + response.data.status_code + "] " + response.data.status;
					$('#activation-form').animate({
						scrollTop: 0
					}, 500);
				}
			});
		} else {
			_activate();
		}
	}

	function setWidth() {
		$scope.width = window.innerWidth;
	}
	setWidth();
	$(window).on('resize', setWidth);
});
// Enable disabled sites controller
// url: /disabled
app.controller('DisabledSitesCtrl', function($scope, $location, $window,
	AuthService, SitesService, UserInfo) {
	UserInfo.updateToken(); // Updates user access token
	// Redirect if not logged in
	setInterval(function() {
		UserInfo.verify(function(response, errored) {
			if (errored === false) {
				$scope.accessToken = null; delete $scope.accessToken;
				$window.location.href = "/login";
				return;
			}
			$scope.accessToken = $window.localStorage.getItem('token');
		});
	}, 60000);

	AuthService.getAuthorization().then(function(response) {
		$scope.permissionLevel = response.data.level;
		if ($scope.addSiteLevel == null) {
			$scope.canCreateSite = true;
			return;
		}

		for (var i = 0; i < $scope.addSiteLevel.length; i++)
			if ($scope.addSiteLevel[i] == $scope.permissionLevel)
				$scope.canCreateSite = true;
	});

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/disable";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.permissionLevel = response.data.level;
	});

	$scope.sites = [];
	SitesService.disabledSites().then(function(response) {
		if (response.data.status_code == 0)
			$scope.sites = response.data.report;
	});

	$scope.editSite = function(id, event) {
		if (id == null) return;
		if (event.ctrlKey || event.type == "auxclick") {
			var win = window.open("/site/" + id, '_blank');
			win.focus();
		} else
			$location.path('/site/' + id).search({});
	}

	$scope.query = {
		order: 'siteNumber',
		limit: 99999,
		page: 1
	}
	$scope.filter = {};
	$scope.limitOptions = [25, 50, 75, 100, {
		label: 'All',
		value: function() {
			return $scope.sites ? $scope.sites.length : 0;
		}
	}];

	$scope.previous = function() {
		if (($scope.query.page - 1) > 0)
			$scope.query.page = $scope.query.page - 1;
	}
	$scope.next = function() {
		if (($scope.query.page) * $scope.query.limit < $scope.users.length)
			$scope.query.page = $scope.query.page + 1;
	}

	// Fixes bug: when on page 2+ and searching, if search result doesn't generate 2+ pages of search results,
	// no results will be rendered
	$scope.searchQueryChanged = function() {
		if ($scope.filter.searchText.trim() != "")
			$scope.query.page = 1;
	}
});
// Event logs page controller
// url: /logs
app.controller('LogsCtrl', function($scope, $location, $window, $mdDialog, UserInfo, LogService) {
	$scope.filter = {
		users: [],
		actions: [],
		dateRange: {
			start: Date.today().addMonths(-1),
			end: Date.today(),
		}
	}
	$scope.today = Date.today();

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/logs";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.permissionLevel = response.data.level;
	});

	function setProperFilterTime() {
		$scope.filter.dateRange.start.setHours(0); $scope.filter.dateRange.start.setMinutes(0); $scope.filter.dateRange.start.setSeconds(0);
		$scope.filter.dateRange.end.setHours(23); $scope.filter.dateRange.end.setMinutes(59); $scope.filter.dateRange.end.setSeconds(59);
		updateLogs();
	}
	setProperFilterTime();

	$scope.dateChanged = setProperFilterTime;

	$scope.query = {
		order: 'siteNumber',
		limit: 25,
		page: 1
	}
	$scope.limitOptions = [25, 50, 75, 100, {
		label: 'All',
		value: function() {
			return $scope.sites ? $scope.sites.length : 0;
		}
	}];

	$scope.previous = function() {
		if (($scope.query.page - 1) > 0)
			$scope.query.page = $scope.query.page - 1;
	}
	$scope.next = function() {
		if (($scope.query.page) * $scope.query.limit < $scope.sites.length)
			$scope.query.page = $scope.query.page + 1;
	}
	// Set limit per page to 25 so it's less intensive for mobile devices
	if (mobileAndTabletcheck())
		$scope.query.limit = 25;

	// Translates Enum nameing style to normal sentence (ex: "ThisIsAVariable" = "This Is A Variable")
	function translateCase(input) {
		var result = input.charAt(0).toUpperCase();
		for (var i = 1; i < input.length; i++)
			if (input.charAt(i).toUpperCase() == input.charAt(i))
				result += " " + input.charAt(i);
			else
				result += input.charAt(i)
		return result;
	}

	function updateLogs() {
		var _date = new Date();
		LogService.getLogs(
			$scope.filter.dateRange.start.addMinutes(_date.getTimezoneOffset()),
			$scope.filter.dateRange.end.addMinutes(_date.getTimezoneOffset())
		).then(function(response) {
			if (response.data.status_code == 0) {
				$scope.request = response.data;

				for (var i = 0; i < $scope.request.actions.length; i++) {
					$scope.request.actions[i].label = translateCase($scope.request.actions[i].label);
				}

				// Translate ID values to names
				for (var i = 0; i < $scope.request.logs.length; i++) {
					// Translate action ID to name
					for (var k = 0; k < $scope.request.actions.length; k++) {
						if ($scope.request.logs[i].action == $scope.request.actions[k].id)
							$scope.request.logs[i].actionName = $scope.request.actions[k].label;
					}
					// Translate user ID to name
					for (var k = 0; k < $scope.request.users.length; k++) {
						if ($scope.request.logs[i].userId == $scope.request.users[k].id)
							$scope.request.logs[i].userName = $scope.request.users[k].userName;
					}

					$scope.request.logs[i].logTime = Date.parse($scope.request.logs[i].logTime).addMinutes(-_date.getTimezoneOffset());
				}
			} else {
				$mdToast.show(
					$mdToast.simple()
						.textContent("[" + response.data.status_code + "] " + response.data.status)
						.position("top right")
						.hideDelay(4000)
				);
			}
		});
	}
	updateLogs();

	$scope.viewLog = function(log, ev) {
		$mdDialog.show({
			locals: { log: log },
			controller: LogDetailCtrl,
			templateUrl: 'tmpl/log-view.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			if (typeof response === "string")
				$window.location.href = response;
		});
	}

	$scope.setTableHeight = function() {
		var tableHeight = 56; // 56 = height of table header row
		var tableWrapper = document.getElementById('tableWrapper');
		var tableBody = document.getElementById('tableBody');
		if (tableWrapper == null || tableBody == null) return; // page hasn't loaded yet

		for (var i = 0; i < tableBody.children.length; i++)
			tableHeight += tableBody.children[i].offsetHeight;

		tableWrapper.style.minHeight = tableHeight + 'px';
	}

	setInterval(function() {
		$scope.setTableHeight();
	}, 500);
});
// Detailed log viewer
// url: /logs (pop-up that only is accessible on this page)
function LogDetailCtrl($scope, $mdDialog, $window, log, UserInfo) {
	UserInfo.updateToken();

	// Check if log has any changes (i.e. is log "Modify *"), and convert to array if so
	// Format: [(KEY=VALUE)][(KEY=VALUE)]...
	if (log.changes.length > 0 && typeof log.changes === "string") {
		log.changes = log.changes.substring(2, log.changes.length - 2).split(')][(');
		angular.forEach(log.changes, function(l, index) {
			for (var i = 0; i < l.length; i++)
				if (l.charAt(i) == '=') {
					log.changes[index] = l.substring(0, i) + ' = ' + l.substring(i + 1);
					return;
				}
		});
	}

	$scope.close = function() {
		$mdDialog.hide(false);
	}
	$scope.isDisabled = function(log) {
		// Will return true if:
		//	a) Something was deleted (can't view data that isn't there anymore)
		//	b) Info involves something that has no page to view info (i.e. installers and managers don't have a page to view info)
		switch (log.action) {
			case 2: // delete user
			case 5: // delete site
			case 7: // create installer
			case 8: // modify installer
			case 9: // delete installer
			case 10: // create manager
			case 11: // modify manager
			case 12: // delete manager
				return true;
			default:
				return false;
		}
	}
	$scope.viewEvent = function() {
		switch (log.action) {
			case 0: // create user
			case 1: // edit user
				$mdDialog.hide('/edit-user/' + log.modifiedId);
				break;
			case 3: // create site
			case 4: // edit site
				$mdDialog.hide('/site/' + log.modifiedId);
				break;
			case 13: // activate site
				$mdDialog.hide('/site/' + log.modifiedId + "?tab=2");
				break;
			case 14: // create adjustment
			case 15: // claim adjustment
			case 16: // complete adjustment
				$mdDialog.hide('/site/' + log.modifiedId + "?tab=3");
				break;
			default: // something that doesn't have a page to view info
				$mdDialog.hide(false);
				break;
		}
	}

	$scope.log = log;
}
// Adjustment view page - shows every adjustment there is
// url: /adjustments
app.controller('AdjustmentsCtrl', function($scope, $location, $window, $mdDialog, AdjustmentService, UserInfo) {
	//$scope.adjustments = [];
	AdjustmentService.getAll().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.adjustments = response.data.report;
			angular.forEach($scope.adjustments, function(value, key) {
				switch (value.type) {
					case 0:
						value.adjustmentType = "Small Increase";
						break;
					case 1:
						value.adjustmentType = "Medium Increase";
						break;
					case 2:
						value.adjustmentType = "Large Increase";
						break;
					case 3:
						value.adjustmentType = "Small Decrease";
						break;
					case 4:
						value.adjustmentType = "Medium Decrease";
						break;
					case 5:
						value.adjustmentType = "Large Decrease";
						break;
					case -1:
						value.adjustmentType = "Reset Only";
						break;
				}
			});
		}
	});

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/adjustments";
			return;
		}
		$scope.accessToken = $window.localStorage.getItem('token');
		$scope.permissionLevel = response.data.level;
	});

	$scope.query = {
		order: '-submissionDate'
	}

	$scope.rowClicked = function(adjustment, ev) {
		$mdDialog.show({
			locals: { adjustment: adjustment },
			controller: AdjustmentInfoCtrl,
			templateUrl: 'tmpl/adjustment-info.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true
		}).then(function(response) {
			if (typeof response === "string")
				$window.location.href = response;
		});
	}
});
function AdjustmentInfoCtrl($scope, $location, $mdDialog, adjustment, UserInfo) {
	// Create copy of adjustment to parse info in a specific way
	adjustment = angular.extend({}, adjustment);
	adjustment.weekMoneyHold = adjustment.weekMoneyIn - adjustment.weekMoneyOut;
	adjustment.weekMoneyPercent = adjustment.weekMoneyHold / adjustment.weekMoneyIn;
	adjustment.monthMoneyHold = adjustment.monthMoneyIn - adjustment.monthMoneyOut;
	adjustment.monthMoneyPercent = adjustment.monthMoneyHold / adjustment.monthMoneyIn;
	adjustment.timeRan = adjustment.timeRan ? Date.parse(adjustment.timeRan) : null;
	adjustment.restartTime = adjustment.restartTime ? Date.parse(adjustment.restartTime) : null;
	adjustment.submissionDate = adjustment.submissionDate ? Date.parse(adjustment.submissionDate) : null;
	adjustment.completedDate = adjustment.completedDate ? Date.parse(adjustment.completedDate) : null;
	$scope.adjustment = adjustment;

	$scope.close = function() {
		$mdDialog.hide();
	}
	$scope.viewAdjustment = function() {
		$location.path("/site/" + adjustment.siteId).search({ 'tab': 3 });
		$mdDialog.hide();
	}
}
// Tickets page - shows all submitted tickets
// url: /tickets
app.controller('TicketsCtrl', function($scope, $window, $mdDialog, $mdToast,
	TicketService, TicketCommentService, UserInfo) {

	$scope.newComment = {
		ticketId: -1,
		poster: -1,
		comment: ""
	};

	UserInfo.verify(function(response, errored) {
		if (errored === false) {
			$scope.accessToken = null; delete $scope.accessToken;
			$window.location.href = "/login?redirectTo=/tickets";
			return;
		}
		$scope.authUser = response.data;
		$scope.newComment.poster = response.data.subject;
		$scope.accessToken = $window.localStorage.getItem('token');
	});

	$scope.filter = {
		yourTickets: false,
		showClosedTickets: false
	};

	// Comment-related functions
	$scope.setCommentId = function(id) {
		$scope.newComment.ticketId = id;
	}
	$scope.showComments = function(ticket) {
		ticket.showComments = !ticket.showComments;
	}
	$scope.submitComment = function() {
		if ($scope.newComment.ticketId == -1 || $scope.newComment.poster == -1) return;
		TicketCommentService.createTicketComment($scope.newComment).then(function(response) {
			if (response.data.status_code == 0) {
				var ticketComment = response.data.ticketComment;
				angular.forEach($scope.tickets, function(ticket, index) {
					if (ticket.id == ticketComment.ticketId)
						ticket.ticketComments.push(ticketComment);
				});
			}
		});
	}

	$scope.now = Date.now();
	$scope.getStatusColor = function(ticket) {
		/*public enum TicketStatus {
			NEW = 0, OPEN = 1, AWAITING = 2,
			IN_PROGRESS = 3, CLOSED = 4
		}*/
		switch (ticket.status) {
			case 1:
				return 'rgba(0, 127, 0, 255)';
			case 2:
			case 3:
				return 'rgba(127, 127, 127, 255)';
			case 4:
				return 'rgba(0, 0, 0, 255)';
			default:
				return 'rgba(0, 0, 255, 255)';
		}
	}
	$scope.getPriorityColor = function(ticket) {
		/*public enum PriortiyLevel {
			LOW = 0, MEDIUM = 1,
			HIGH = 2, VERY_HIGH = 3
		}*/
		switch (ticket.priority) {
			case 1:
				return 'rgba(255, 127, 0, 255)';
			case 2:
				return 'rgba(255, 0, 0, 255)';
			case 3:
				return 'rgba(255, 0, 64, 255)';
			default:
				return 'rgba(0, 127, 0, 255)';
		}
	}
	$scope.getCategoryColor = function(ticket) {
		switch (ticket.category) {
			case 0:
			case 1:
			case 2:
				return 'rgba(0, 0, 255, 255)';
			case 3:
			case 4:
			case 6:
				return 'rgba(0, 127, 0, 255)';
			default:
				return 'rgba(127, 0, 255, 255)';
		}
	};
	$scope.tickets = [];

	$scope.create = function(ev) {
		$mdDialog.show({
			controller: CreateTicketCtrl,
			templateUrl: 'tmpl/ticket-view.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true,
			locals: { authUser: $scope.authUser }
		}).then(function(dialogResponse) {
			console.log(dialogResponse);
			if (dialogResponse) {
				dialogResponse.dueDate = dialogResponse.dueDate ? dialogResponse.dueDate.toString("yyyy-MM-ddTHH:mm:ss.000z") : null;
				TicketService.submitTicket(dialogResponse).then(function(response) {
					if (response.data.status_code == 0) {
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully created ticket")
								.position("top right")
								.hideDelay(3000)
						);
						$scope.tickets.push(response.data.ticket);
					}
				});
			}
		});
	}
	$scope.update = function(ticket, ev) {
		$mdDialog.show({
			controller: UpdateTicketCtrl,
			templateUrl: 'tmpl/ticket-view.tmpl.html',
			parent: angular.element(document.body),
			targetEvent: ev,
			clickOutsideToClose: true,
			locals: { ticket: ticket, authUser: $scope.authUser }
		}).then(function(dialogResponse) {
			if (dialogResponse) {
				dialogResponse.dueDate = dialogResponse.dueDate ? dialogResponse.dueDate.toString("yyyy-MM-ddTHH:mm:ss.000z") : null;
				TicketService.updateTicket(dialogResponse).then(function(response) {
					if (response.data.status_code == 0) {
						$mdToast.show(
							$mdToast.simple()
								.textContent("Successfully updated ticket")
								.position("top right")
								.hideDelay(3000)
						);
						for (var i = 0; i < $scope.tickets.length; i++)
							if ($scope.tickets[i].id == response.data.ticket.id) {
								$scope.tickets[i] = response.data.ticket;
								break;
							}
					}
				});
			}
		});
	}
	$scope.delete = function(ticket, ev) {
		var confirm = $mdDialog.confirm()
			.title('Are you sure?')
			.textContent('You cannot recover deleted tickets.')
			.ariaLabel('Delete ticket')
			.targetEvent(ev)
			.ok('Yes')
			.cancel('No');

		$mdDialog.show(confirm).then(function() {
			TicketService.deleteTicket(ticket.id).then(function(response) {
				if (response.data.status_code == 0) {
					$mdToast.show(
						$mdToast.simple()
							.textContent('Successfully deleted ticket')
							.position("top right")
							.hideDelay(3000)
					);
					for (var i = 0; i < $scope.tickets.length; i++)
						if ($scope.tickets[i].id == ticket.id) {
							$scope.tickets.splice(i, 1);
							break;
						}
				}
			});
		});
	};

	TicketService.getAllTickets().then(function(response) {
		if (response.data.status_code == 0) {
			$scope.tickets = response.data.report.sort(function(x, y) {
				x = Date.parse(x.createdDate); y = Date.parse(y.createdDate);
				return x.compareTo(y);
			});
			angular.forEach($scope.tickets, function(ticket) {
				ticket.dueDate = Date.parse(ticket.dueDate);
				ticket.showComments = false;
				ticket.lastUpdated = Date.parse(ticket.lastUpdated);
				ticket.statusName = ticket.statusName.replace('_', ' ');
				ticket.priorityName = ticket.priorityName.replace('_', ' ');
				ticket.ticketComments = [];
				if (ticket.dueDate.getFullYear() == 2000)
					ticket.dueDate = null;
				if (ticket.lastUpdated.getFullYear() == 2000)
					ticket.lastUpdated = null;

				TicketCommentService.getTicketComments(ticket.id).then(function(response) {
					if (response.data.status_code == 0) {
						ticket.ticketComments = response.data.report;
						ticket.ticketComments.sort(function(x, y) {
							x = Date.parse(x.time); y = Date.parse(y.time);
							return x.compareTo(y);
						});
					} else
						ticket.ticketComments = [{ "posterName": "[System]", "poster": 0, "comment": "Unable to get comments for this ticket" }];
				});
			});
			$scope.tickets.sort(function(x, y) {
				if (x.dueDate === null)
					return 1;
				else if (y.dueDate === null)
					return -1;
				else if (x.dueDate < y.dueDate)
					return -1;
				else if (x.dueDate > y.dueDate)
					return 1;
				return 0;
			});
		} else {
			$mdToast.show(
				$mdToast.simple()
					.textContent("[" + response.data.status_code + "] " + response.data.status)
					.position("top right")
					.hideDelay(3000)
			);
		}
	});
});
function CreateTicketCtrl($scope, $mdDialog, $window, $mdToast, authUser,
	SitesService, TicketService, UserService, UserInfo) {
	$scope.ticket = { status: 0 };
	$scope.authUser = authUser;
	$scope.errorMsg = null;
	$scope.submitButton = "Create";

	$scope.close = function() {
		$mdDialog.hide();
	}
	$scope.button = function() {
		$scope.errorMsg = null;
		if ($scope.ticket.subject == null || $scope.ticket.subject.length == 0)
			$scope.errorMsg = "Please enter ticket subject";
		else if ($scope.ticket.comments == null || $scope.ticket.comments.length == 0)
			$scope.errorMsg = "Please enter ticket comments";
		else if ($scope.ticket.priority == null)
			$scope.errorMsg = "Please select ticket priority";
		else if ($scope.ticket.category == null)
			$scope.errorMsg = "Please select ticket category";
		else {
			$scope.errorMsg = null;
			$mdDialog.hide($scope.ticket);
		}
	}

	SitesService.listAll().then(function(response) {
		if (response.data.status_code == 0)
			$scope.sites = response.data.report;
	});
	UserService.highLevelUsers().then(function(response) {
		if (response.data.status_code == 0)
			$scope.ticketAssignees = response.data.users;
	});
}
function UpdateTicketCtrl($scope, $mdDialog, $window, $mdToast, ticket, authUser,
	SitesService, TicketService, UserService, UserInfo) {
	$scope.ticket = angular.extend({}, ticket);
	$scope.authUser = authUser;
	$scope.submitButton = "Update";
	if (ticket.dueDate != null)
		ticket.hasDueDate = true;

	$scope.close = function() {
		$mdDialog.hide();
	}
	$scope.button = function() {
		$scope.errorMsg = null;
		if ($scope.ticket.subject == null || $scope.ticket.subject.length == 0)
			$scope.errorMsg = "Please enter ticket subject";
		else if ($scope.ticket.comments == null || $scope.ticket.comments.length == 0)
			$scope.errorMsg = "Please enter ticket comments";
		else if ($scope.ticket.priority == null)
			$scope.errorMsg = "Please select ticket priority";
		else if ($scope.ticket.category == null)
			$scope.errorMsg = "Please select ticket category";
		else {
			$scope.errorMsg = null;
			$mdDialog.hide($scope.ticket);
		}
	}

	SitesService.listAll().then(function(response) {
		if (response.data.status_code == 0)
			$scope.sites = response.data.report;
	});
	UserService.highLevelUsers().then(function(response) {
		if (response.data.status_code == 0)
			$scope.ticketAssignees = response.data.users;
	});
}