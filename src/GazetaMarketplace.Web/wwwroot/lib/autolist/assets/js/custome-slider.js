let carouselCountainer = document.querySelectorAll('.carousel');
carouselCountainer.forEach((cc)=>{
let carouselID = cc.getAttribute('id');
let carouselMain = document.querySelector(`#${carouselID}.carousel`);
let carouselItem = document.querySelectorAll(`#${carouselID} .carousel-inner .carousel-item`);
let ThumbPresent = cc.getAttribute('data-bs-thumb');
let DotsPresent = cc.getAttribute('data-bs-dots');
let indicatorClass = cc.getAttribute('data-bs-indicator-class');
if(ThumbPresent == "true"){
	

// creating Carousel Indicators dynamically

// carouselIndicatorSlideWrap 
let carouselIndicatorSlideWrap = document.createElement('div');
carouselIndicatorSlideWrap.setAttribute('class', 'slider-wrap');
//carouselIndicatorSlide
let carouselIndicatorSlide = document.createElement('div');
carouselIndicatorSlide.setAttribute('class', 'slider');		
// slider-inner(carouselIndicators)
let carouselIndicators = document.createElement('div');
if(indicatorClass !== null){
	console.log(typeof(indicatorClass));
	carouselIndicators.setAttribute('class', `slider-inner carousel-indicators ${indicatorClass}`);
}
else{
	carouselIndicators.setAttribute('class', `slider-inner carousel-indicators`);
}

// arrows
let prevArrow = document.createElement('a');
prevArrow.setAttribute('class', 'carousel-control-prev');
let leftIcon = document.createElement('i');
leftIcon.setAttribute('class','fa fa-angle-left');
prevArrow.append(leftIcon);

let nextArrow = document.createElement('a');
nextArrow.setAttribute('class', 'carousel-control-next');
let rightIcon = document.createElement('i');
rightIcon.setAttribute('class','fa fa-angle-right');
nextArrow.append(rightIcon);


// appending
carouselIndicatorSlide.append(carouselIndicators);
carouselIndicatorSlideWrap.append(carouselIndicatorSlide);
carouselIndicatorSlideWrap.append(prevArrow);
carouselIndicatorSlideWrap.append(nextArrow);
carouselMain.append(carouselIndicatorSlideWrap);

// let carouselIndicators
carouselItem.forEach((el,ind)=>{
	let childrenImg =  el.innerHTML;
	let Htmlcode =  `<div data-bs-target="#${carouselID}" data-bs-slide-to="${ind}" data-bs-active-id="next" class="thumb item">${childrenImg}</div>`;
	carouselIndicators.innerHTML += Htmlcode	;			
});
let carouselIndicatorNode = document.querySelectorAll(`#${carouselID} .carousel-indicators .thumb.item`);
carouselIndicatorNode[0].classList.add('active');
const slider = document.querySelector(`#${carouselID} .slider`);
const sliderInner = document.querySelector(`#${carouselID} .slider-inner`);

	let carouselIndicatorSlideWidth;
	let currentTranslate;
	function checkCarouselIndicatorWidth(){
			carouselIndicatorSlideWidth =  slider.offsetWidth;

			let dir = document.querySelector('html').getAttribute('dir')
		
			let eValue = Math.ceil(window.getComputedStyle(slider.firstElementChild).transform.split(',')[4])
			let sliderChildWidth = slider.firstElementChild.scrollWidth - carouselIndicatorSlideWidth;
			if(eValue < -sliderChildWidth && dir !== "rtl"){
				currentTranslate = -sliderChildWidth;
				slider.firstElementChild.style.transform = `translateX(${currentTranslate}px)`
			}
			else if(eValue > sliderChildWidth && dir == "rtl"){
				currentTranslate = sliderChildWidth;
				slider.firstElementChild.style.transform = `translateX(${currentTranslate}px)`
			}
	}
	checkCarouselIndicatorWidth();
	window.addEventListener('resize', function(event) {
		checkCarouselIndicatorWidth();
	}, true);

	let scrollOffsetWidth = slider.scrollWidth - slider.offsetWidth ;
	let dir = document.querySelector('html').getAttribute('dir');
	
	directionCheck(dir, carouselMain, carouselIndicators, prevArrow, nextArrow, carouselID,sliderInner,slider, carouselIndicatorSlide);
	
	
	/***************** RTL *********************/
	$('#myonoffswitch55').click(function () {
		directionCheck("rtl",carouselMain, carouselIndicators, prevArrow, nextArrow, carouselID,sliderInner,slider, carouselIndicatorSlide);

	});

	/***************** RTL *********************/

	/***************** LTR *********************/

	$('#myonoffswitch54').click(function () {
		directionCheck("ltr",carouselMain,carouselIndicators,prevArrow, nextArrow, carouselID,sliderInner,slider, carouselIndicatorSlide);
	});


	/***************** LTR *********************/
}
else if(DotsPresent == "true"){
// creating Carousel Indicators dynamically

	// carouselIndicatorSlideWrap 
	let carouselIndicatorSlideWrap = document.createElement('div');
	carouselIndicatorSlideWrap.setAttribute('class', 'slider-wrap');
	//carouselIndicatorSlide
	let carouselIndicatorSlide = document.createElement('div');
	carouselIndicatorSlide.setAttribute('class', 'slider');		
	// slider-inner(carouselIndicators)
	let carouselIndicators = document.createElement('div');
	carouselIndicators.setAttribute('class', "slider-inner carousel-indicators");

	// arrows
	let prevArrow = document.createElement('a');
	prevArrow.setAttribute('class', 'carousel-control-prev');
	let leftIcon = document.createElement('i');
	leftIcon.setAttribute('class','fa fa-angle-left');
	prevArrow.append(leftIcon);

	let nextArrow = document.createElement('a');
	nextArrow.setAttribute('class', 'carousel-control-next');
	let rightIcon = document.createElement('i');
	rightIcon.setAttribute('class','fa fa-angle-right');
	nextArrow.append(rightIcon);


	// appending
	carouselIndicatorSlide.append(carouselIndicators);
	carouselIndicatorSlideWrap.append(carouselIndicatorSlide);
	carouselMain.append(carouselIndicatorSlideWrap);

	// let carouselIndicators
	carouselItem.forEach((el,ind)=>{
		let Htmlcode =  `<div data-bs-target="#${carouselID}" data-bs-slide-to="${ind}" data-bs-active-id="next" class="dots"></div>`
		carouselIndicators.innerHTML += Htmlcode				
	})
	let carouselIndicatorNode = document.querySelectorAll(`#${carouselID} .carousel-indicators .dots`);
	carouselIndicatorNode[0]?.classList.add('active')
	
}

})


function directionCheck(dir,carouselMain,carouselIndicators,prevArrow,nextArrow, carouselID, sliderInner, slider, carouselIndicatorSlide){
	if(dir == "ltr" || dir == null || dir == "" || dir !== "rtl"){
		var currentTranslate;
		let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);
		if(translateXValue > 0 ){
			currentTranslate = -translateXValue
			sliderInner.style.transform = `translateX(${currentTranslate}px)`
		}
		else{
			currentTranslate = translateXValue
			sliderInner.style.transform = `translateX(${currentTranslate}px)`
		}
		nextArrow.addEventListener('click',(e)=>{
            let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);
            
			let carouselIndicatorSlideWidth = carouselIndicatorSlide.offsetWidth;
			let check = sliderInner.getBoundingClientRect().width + (0 -  slider.offsetWidth);
			if(translateXValue > -check){
				currentTranslate = translateXValue - carouselIndicatorSlideWidth;
				if( currentTranslate < -check){
					currentTranslate = -check
					sliderInner.style.transform = `translateX(${currentTranslate}px)`

					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						nextArrow.classList.add('btn-disabled');
						prevArrow.classList.remove('btn-disabled');
					}
				}
				else{
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						nextArrow.classList.remove('btn-disabled');
						prevArrow.classList.remove('btn-disabled');
					}
				}
			}
			else{
				if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
					nextArrow.classList.add('btn-disabled');
					prevArrow.classList.remove('btn-disabled');
					return false;
				}
				else if(carouselMain.getAttribute('data-bs-loop') == "true"){
					currentTranslate = 0;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					prevArrow.classList.remove('btn-disabled');
				}
			}
		})

		prevArrow.addEventListener('click',(e)=>{
            let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);

			let carouselIndicatorSlideWidth = carouselIndicatorSlide.offsetWidth;
			let check = sliderInner.getBoundingClientRect().width + (0 -  slider.offsetWidth);
			if(translateXValue === 0 ){
				if(carouselMain.getAttribute('data-bs-loop') !== 'true'){
					prevArrow.classList.remove('btn-disabled');
					nextArrow.classList.remove('btn-disabled');
					
				}
				else{
					currentTranslate = -check;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
				}
			}
			else if(translateXValue >= -check){
				currentTranslate = (translateXValue + carouselIndicatorSlideWidth);
				if(currentTranslate >= 0){
					currentTranslate = 0;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						prevArrow.classList.add('btn-disabled');
						nextArrow.classList.remove('btn-disabled');
					}
				}
				else{
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						prevArrow.classList.remove('btn-disabled');
						nextArrow.classList.remove('btn-disabled');
					}
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
				}
			}
		})

		function checkBtn(){
		let translateXValue =  Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);
			if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
				if(translateXValue == 0){
					prevArrow.classList.add('btn-disabled');
				}
			}
		}
		checkBtn();

		let thumbSelect = document.querySelectorAll(`#${carouselID} .thumb.item`);
		let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);

		function getNextSiblingsElementList(elem) {
			var nextSibs = [];
				while (elem = elem.nextElementSibling) {
					elem.setAttribute('data-bs-active-id',"next")
					nextSibs.push(elem);
				}
				return nextSibs;
		}
		function getPreviousSiblingsElementList(elem) {
			var prevSibs = [];
			while (elem = elem.previousElementSibling) {
					elem.setAttribute('data-bs-active-id',"prev")
				prevSibs.push(elem);
			}
			return prevSibs;
		}
		function incrementValue(e){
			let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);
			var style = e.currentStyle || window.getComputedStyle(e);
			let totalImgWidth = e.offsetWidth + parseFloat(style.marginLeft) + parseFloat(style.marginRight);
			let ss = document.querySelector(`#${carouselID} .slider`)
			let scrollOffsetWidth = ss.firstElementChild.scrollWidth - ss.offsetWidth ;
			if(e.offsetLeft < 190 && window.innerWidth > 425){
				currentTranslate = 0;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
			}
			else if(-e.offsetLeft < -check){
				currentTranslate = -check;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.add('btn-disabled');
			}
			else if(e.offsetLeft - slider.offsetWidth >= scrollOffsetWidth ){
				currentTranslate = -check;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.add('btn-disabled');
			}
			else if(window.innerWidth < 425 && e.offsetLeft < 190){
				currentTranslate = (-e.offsetLeft) + (totalImgWidth * 0);
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
			}
			else if(window.innerWidth < 425 && e.offsetLeft > 190){
				currentTranslate = (-e.offsetLeft) + (totalImgWidth * 0);
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.remove('btn-disabled');
			}
			else{
				currentTranslate = (-e.offsetLeft) + (totalImgWidth * 1);
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.remove('btn-disabled');
			}
		}
		function decrementValue(e){
			var style = e.currentStyle || window.getComputedStyle(e);
			let totalImgWidth = e.offsetWidth + parseFloat(style.marginLeft) + parseFloat(style.marginRight);
			let ss = document.querySelector(`#${carouselID} .slider`)
			let scrollOffsetWidth = ss.firstElementChild.scrollWidth - ss.offsetWidth ;
			let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);
			if((e.offsetLeft - (totalImgWidth * 2)) > scrollOffsetWidth && window.innerWidth > 425){
				currentTranslate = -check;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.add('btn-disabled');
			}
			else if( -e.offsetLeft > -(totalImgWidth * 2) ){
				currentTranslate = 0;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.add('btn-disabled');
				nextArrow.classList.remove('btn-disabled');
			}
			else if(window.innerWidth < 425){
				currentTranslate =( -e.offsetLeft) + (totalImgWidth * 1);
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
			}
			else{
				currentTranslate =( -e.offsetLeft) + (totalImgWidth * 2);
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.remove('btn-disabled');
			}	
		}


		let observer;
		thumbSelect.forEach((e, i)=>{
			let ln = thumbSelect.length
			function callBackFn(mutationList, observer){
				mutationList.forEach((mutation)=>{
					let checkActive = mutation.target.className.includes('active')
					if(checkActive == true){
						getNextSiblingsElementList(e);
						getPreviousSiblingsElementList(e);
						if(mutation.target.getAttribute('data-bs-active-id') == "next"){
							incrementValue(mutation.target);
						}
						else if(mutation.target.getAttribute('data-bs-active-id') == "prev"){
							decrementValue(mutation.target);
						}
						// to bring it back to start
						if(mutation.target.offsetLeft > 0 && mutation.target.offsetLeft <25){
							currentTranslate = 0;
							sliderInner.style.transform = `translateX(${currentTranslate}px)`;
						}
						// to send it to end
						if(mutation.target.offsetLeft + 110 == sliderInner.scrollWidth){
							currentTranslate = -check;
							sliderInner.style.transform = `translateX(${currentTranslate}px)`;
						}
					}
				})
			}
			var MutationObserver = window.MutationObserver || window.WebKitMutationObserver || window.MozMutationObserver;
			const options = {attributes: true};
			observer = new MutationObserver(callBackFn);
			observer.observe(e, options)


		})
	}
	else{
		var currentTranslate;
		let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);
		currentTranslate = Math.abs(translateXValue);
		sliderInner.style.transform = `translateX(${currentTranslate}px)`
		nextArrow.addEventListener('click',(e)=>{
            let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);

			let check = sliderInner.getBoundingClientRect().width + (0 -  slider.offsetWidth);
			let carouselIndicatorSlideWidth = carouselIndicatorSlide.offsetWidth;
			if(translateXValue < check){
				currentTranslate = translateXValue + carouselIndicatorSlideWidth;
				if( currentTranslate > check){
					currentTranslate = check
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
		
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						nextArrow.classList.add('btn-disabled');
						prevArrow.classList.remove('btn-disabled');
					}
				}
				else{
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						nextArrow.classList.remove('btn-disabled');
						prevArrow.classList.remove('btn-disabled');
					}
				}
			}
			else{
				if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
					nextArrow.classList.add('btn-disabled');
					prevArrow.classList.remove('btn-disabled');
					return false;
				}
				else if(carouselMain.getAttribute('data-bs-loop') == "true"){
					currentTranslate = 0;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					prevArrow.classList.remove('btn-disabled');
				}
			}
		})
		prevArrow.addEventListener('click',(e)=>{
        let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);

			let carouselIndicatorSlideWidth = carouselIndicatorSlide.offsetWidth;
			let check = sliderInner.getBoundingClientRect().width + (0 -  slider.offsetWidth);
			if(translateXValue == 0 ){
				if(carouselMain.getAttribute('data-bs-loop') !== 'true'){
					prevArrow.classList.remove('btn-disabled');
					nextArrow.classList.remove('btn-disabled');
					
				}
				else{
					currentTranslate = check;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
				}
			}
			else if(translateXValue <= check){
				currentTranslate = (translateXValue - carouselIndicatorSlideWidth);
				if(currentTranslate > 0){
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						prevArrow.classList.remove('btn-disabled');
						nextArrow.classList.remove('btn-disabled');
					}
					sliderInner.style.transform = `translateX(${currentTranslate}px)`			
				}
				else{
					currentTranslate = 0;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`
					
					if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
						prevArrow.classList.add('btn-disabled');
						nextArrow.classList.remove('btn-disabled');
					}
				}
			}
		})
		function checkBtn(){
		let translateXValue = Math.ceil(window.getComputedStyle(carouselIndicators).transform.split(',')[4]);
			if(carouselMain.getAttribute('data-bs-loop') == null || carouselMain.getAttribute('data-bs-loop') == "false" ){
				if(translateXValue == 0){
					prevArrow.classList.add('btn-disabled');
				}
			}
		}
		checkBtn();
		
		let thumbSelect = document.querySelectorAll(`#${carouselID} .thumb.item`);
		let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);
		
		function getNextSiblingsElementList(elem) {
			var nextSibs = [];
				while (elem = elem.nextElementSibling) {
					elem.setAttribute('data-bs-active-id',"next")
					nextSibs.push(elem);
				}
				return nextSibs;
		}
		function getPreviousSiblingsElementList(elem) {
			var prevSibs = [];
			while (elem = elem.previousElementSibling) {
					elem.setAttribute('data-bs-active-id',"prev")
				prevSibs.push(elem);
			}
			return prevSibs;
		}
		function incrementValue(e){
			let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);
			var len = e.parentElement.children.length;
			var style = e.currentStyle || window.getComputedStyle(e);
			let totalImgWidth = e.offsetWidth + parseFloat(style.marginRight) + parseFloat(style.marginRight);
			let ss = document.querySelector(`#${carouselID} .slider`)
			let scrollOffsetWidth = ss.firstElementChild.scrollWidth - ss.offsetWidth ;
			// if(e.offsetLeft < 190){return false;}
			if(- e.offsetLeft + (totalImgWidth * (len-2)) >= check){
				currentTranslate = check;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				nextArrow.classList.add('btn-disabled');
				prevArrow.classList.remove('btn-disabled');
			}
			
			else{
				currentTranslate = -(e.offsetLeft) + (totalImgWidth * (len-2));
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				nextArrow.classList.remove('btn-disabled');
				prevArrow.classList.remove('btn-disabled');
			}
		}
		function decrementValue(e){
			var len = e.parentElement.children.length;
			var style = e.currentStyle || window.getComputedStyle(e);
			let totalImgWidth = e.offsetWidth + parseFloat(style.marginRight) + parseFloat(style.marginRight);
			let ss = document.querySelector(`#${carouselID} .slider`)
			let scrollOffsetWidth = ss.firstElementChild.scrollWidth - ss.offsetWidth ;
			let check = sliderInner?.getBoundingClientRect().width + (0 -  slider?.offsetWidth);
			if(( -e.offsetLeft + (totalImgWidth * (len-2))) > scrollOffsetWidth){
				nextArrow.classList.add('btn-disabled');
				prevArrow.classList.remove('btn-disabled');
				currentTranslate = check;
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
			}
			else{
				prevArrow.classList.remove('btn-disabled');
				nextArrow.classList.remove('btn-disabled');
				currentTranslate =( -e.offsetLeft) + (totalImgWidth * (len - 2));
				if(currentTranslate < 0){
					prevArrow.classList.add('btn-disabled');
					currentTranslate = 0;
					sliderInner.style.transform = `translateX(${currentTranslate}px)`;
				}
				sliderInner.style.transform = `translateX(${currentTranslate}px)`;
			}	
		}
	
	
		let observer;
		thumbSelect.forEach((e, i)=>{
			let ln = thumbSelect.length
			function callBackFn(mutationList, observer){
				mutationList.forEach((mutation)=>{
					let checkActive = mutation.target.className.includes('active')
					if(checkActive == true){
						getNextSiblingsElementList(e);
						getPreviousSiblingsElementList(e);
						if(mutation.target.getAttribute('data-bs-active-id') == "next"){
							incrementValue(mutation.target);
						}
						else if(mutation.target.getAttribute('data-bs-active-id') == "prev"){
							decrementValue(mutation.target);
						}
					}
				})
			}
			const options = {attributes: true};
			observer = new MutationObserver(callBackFn);
			observer.observe(e, options)
	
	
		})
	}
}