function totalbudget () {
	/* Chartjs (#total-budget) */
	
	var myCanvas = document.getElementById("total-budget");
	var myCanvasContext = myCanvas.getContext("2d");
	var gradientStroke1 = myCanvasContext.createLinearGradient(0, 0, 0, 230);
	gradientStroke1.addColorStop(0, hexToRgba(myVarVal, 0.2));
	var myChart = new Chart(myCanvas, {
		type: 'line',
		data: {
			labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
			type: 'line',
			datasets: [{
				label: 'Market value',
				data: [30, 70, 30, 100, 50, 130, 100, 140],
				backgroundColor: gradientStroke1,
				borderColor: [myVarVal],
				pointBackgroundColor: '#fff',
				pointHoverBackgroundColor: gradientStroke1,
				pointBorderColor: gradientStroke1,
				pointHoverBorderColor: gradientStroke1,
				pointBorderWidth: 0,
				pointRadius: 0,
				pointHoverRadius: 0,
				borderWidth: 2
			}, ]
		},
		options: {
			responsive: true,
			maintainAspectRatio: false,
			tooltips: {
				mode: 'index',
				titleFontSize: 12,
				titleFontColor: 'rgba(225,225,225,0.9)',
				bodyFontColor: 'rgba(225,225,225,0.9)',
				backgroundColor: 'rgba(0,0,0,0.7)',
				cornerRadius: 3,
				intersect: false,
			},
			legend: {
				display: false,
				labels: {
					usePointStyle: true,
				},
			},
			scales: {
				xAxes: [{
					ticks: {
						fontColor: "#605e7e",
					},
					display: true,
					gridLines: {
						display: false,
						drawBorder: false
					},
					scaleLabel: {
						display: false,
						labelString: 'Month',
						fontColor: 'rgba(225,225,225,0.5)'
					}
				}],
				yAxes: [{
					ticks: {
						fontColor: "transparent",
					},
					display: true,
					gridLines: {
						display: false,
						drawBorder: false
					},
					scaleLabel: {
						display: false,
						labelString: 'sales',
						fontColor: 'transparent'
					}
				}]
			},
			title: {
				display: false,
				text: 'Normal Legend'
			}
		}
	});

}

function expense () {
	/* Chartjs (#expense) */
	var myCanvas = document.getElementById("expense");
	var myCanvasContext = myCanvas.getContext("2d");
	var gradientStroke1 = myCanvasContext.createLinearGradient(0, 0, 0, 230);
	gradientStroke1.addColorStop(0, myVarVal);
	var myChart = new Chart(myCanvas, {
		type: 'bar',
		data: {
			labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Aug", "Sep"],
			datasets: [{
				label: 'Amount Of Sale',
				data: [16, 14, 12, 14, 16, 15, 12, 14, 18, 10],
				backgroundColor: gradientStroke1,
				hoverBackgroundColor: gradientStroke1,
				hoverBorderWidth: 2,
				hoverBorderColor: 'gradientStroke1'
			}]
		},
		options: {
			responsive: true,
			maintainAspectRatio: false,
			tooltips: {
				mode: 'index',
				titleFontSize: 12,
				titleFontColor: 'rgba(225,225,225,0.9)',
				bodyFontColor: 'rgba(225,225,225,0.9)',
				backgroundColor: 'rgba(0,0,0,0.7)',
				cornerRadius: 3,
				intersect: false,
			},
			legend: {
				display: false,
				labels: {
					usePointStyle: true,
				},
			},
			scales: {
				xAxes: [{
					barPercentage: 0.3,
					ticks: {
						fontColor: "#605e7e",
					},
					display: true,
					gridLines: {
						display: false,
						drawBorder: false
					},
					scaleLabel: {
						display: false,
						labelString: 'Month',
						fontColor: 'rgba(225,225,225,0.5)'
					}
				}],
				yAxes: [{
					ticks: {
						fontColor: "transparent",
					},
					display: true,
					gridLines: {
						display: false,
						drawBorder: false
					},
					scaleLabel: {
						display: false,
						labelString: 'sales',
						fontColor: 'transparent'
					}
				}]
			},
			title: {
				display: false,
				text: 'Normal Legend'
			}
		}
	});
	/* Chartjs (#expense) closed */
}