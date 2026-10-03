$(function () {
	"use strict";

	// ______________Loader 
	$("#global-loader").fadeOut("slow");

	// ______________ Sidebar
	$(document).ready(function() {
		$(".app-sidebar a").each(function() {
			var pageUrl = window.location.href.split(/[?#]/)[0];
			if (this.href == pageUrl) {
				$(this).addClass("active");
				$(this).parent().addClass("is-expanded");
				$(this).parent().addClass("active");
				$(this).parent().parent().prev().addClass("active");
				$(this).parent().parent().addClass("open");
				$(this).parent().parent().prev().addClass("is-expanded");
				$(this).parent().parent().parent().addClass("is-expanded");
				$(this).parent().parent().parent().parent().addClass("open");
				$(this).parent().parent().parent().parent().prev().addClass("active");
				$(this).parent().parent().parent().parent().parent().addClass("is-expanded");
				$(this).parent().parent().parent().parent().parent().parent().prev().addClass("active");
				$(this).parent().parent().parent().parent().parent().parent().parent().addClass("is-expanded");
			}
		});
	});

	// ______________Full screen
	$("#fullscreen-button").on("click", function toggleFullScreen() {
	var inFullScreenMode = document.fullscreenElement ||
    document.mozFullScreenElement || document.webkitFullscreenElement || document.msFullscreenElement;

	if (inFullScreenMode) {
		if (document.exitFullscreen) {
			document.exitFullscreen();
		} else if (document.msExitFullscreen) {
			document.msExitFullscreen();
		} else if (document.mozCancelFullScreen) {
			document.mozCancelFullScreen();
		} else if (document.webkitExitFullscreen) {
			document.webkitExitFullscreen();
		}
	} else {
		var docElm = document.documentElement;
		if (docElm.requestFullscreen) {
			docElm.requestFullscreen();
		} else if (docElm.msRequestFullscreen) {
			docElm.msRequestFullscreen();
		} else if (docElm.mozRequestFullScreen) {
			docElm.mozRequestFullScreen();
		} else if (docElm.webkitRequestFullScreen) {
			docElm.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT);
			//Safari silently fails with the above, use workaround:
			setTimeout(function() {
				if (!document.webkitCurrentFullScreenElement) {
					docElm.webkitRequestFullScreen();
				}
			},200);
		}
	}
	})
	// ______________ Color-skin
	$(document).ready(function() {
		$("a[data-theme]").click(function() {
			$("head link#theme").attr("href", $(this).data("theme"));
			$(this).toggleClass('active').siblings().removeClass('active');
		});
		$("a[data-effect]").click(function() {
			$("head link#effect").attr("href", $(this).data("effect"));
			$(this).toggleClass('active').siblings().removeClass('active');
		});
	});



	// ______________Loader
	$(window).on("load", function(e) {
		$("#loading").fadeOut("slow");
	})

	// ______________Cover Image
	$(".cover-image").each(function() {
		var attr = $(this).attr('data-image-src');
		if (typeof attr !== typeof undefined && attr !== false) {
			$(this).css('background', 'url(' + attr + ') center center');
		}
	});

	// ______________ Back to Top
	$(window).on("scroll", function(e) {
		if ($(this).scrollTop() > 0) {
			$('#back-to-top').fadeIn('slow');
		} else {
			$('#back-to-top').fadeOut('slow');
		}
	});
	$("#back-to-top").on("click", function(e) {
		$("html, body").animate({
			scrollTop: 0
		}, 0);
		return false;
	});

	// ______________Global Search
	$(document).on("click", "[data-bs-toggle='search']", function(e) {
		var body = $("body");
		if (body.hasClass('search-gone')) {
			body.addClass('search-gone');
			body.removeClass('search-show');
		} else {
			body.removeClass('search-gone');
			body.addClass('search-show');
		}
	});
	var toggleSidebar = function() {
		var w = $(window);
		if (w.outerWidth() <= 1024) {
			$("body").addClass("sidebar-gone");
			$(document).off("click", "body").on("click", "body", function(e) {
				if ($(e.target).hasClass('sidebar-show') || $(e.target).hasClass('search-show')) {
					$("body").removeClass("sidebar-show");
					$("body").addClass("sidebar-gone");
					$("body").removeClass("search-show");
				}
			});
		} else {
			$("body").removeClass("sidebar-gone");
		}
	}
	toggleSidebar();
	$(window).resize(toggleSidebar);

	// ______________Chart-circle
	if ($('.chart-circle').length) {
		$('.chart-circle').each(function() {
			let $this = $(this);
			$this.circleProgress({
				fill: {
					color: $this.attr('data-color')
				},
				size: $this.height(),
				startAngle: -Math.PI / 4 * 2,
				emptyFill: '#e7e9f1',
				lineCap: ''
			});
		});
	}

	const DIV_CARD = 'div.card';
	// ______________Tooltip
	// $('[data-bs-toggle="tooltip"]').tooltip();

	// ______________Popover
	// $('[data-bs-toggle="popover"]').popover({
	// 	html: true
	// });

	// ______________Remove Card
	$(document).on('click', '[data-bs-toggle="card-remove"]', function(e) {
		let $card = $(this).closest(DIV_CARD);
		$card.remove();
		e.preventDefault();
		return false;
	});

	// ______________Card Collapse
	$(document).on('click', '[data-bs-toggle="card-collapse"]', function(e) {
		let $card = $(this).closest(DIV_CARD);
		$card.toggleClass('card-collapsed');
		e.preventDefault();
		return false;
	});

	// ______________Card Fullscreen
	$(document).on('click', '[data-bs-toggle="card-fullscreen"]', function(e) {
		let $card = $(this).closest(DIV_CARD);
		$card.toggleClass('card-fullscreen').removeClass('card-collapsed');
		e.preventDefault();
		return false;
	});

	// ______________Counterup
	// $('.counter').countUp();

	// ______________Dropdown
	// $('.dropdown-bs-toggle').dropdown();

});