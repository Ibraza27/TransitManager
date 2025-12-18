window.chartHelper = {
    createRevenueChart: function (canvasId, dataLabels, dataValues) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;

        // Destroy existing chart if any (to prevent overlap on redraw)
        if (ctx.chartInstance) {
            ctx.chartInstance.destroy();
        }

        ctx.chartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: dataLabels,
                datasets: [{
                    label: 'Chiffre d\'Affaires (€)',
                    data: dataValues,
                    borderColor: '#0d6efd', // Bootstrap Primary
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2,
                    tension: 0.4, // Smooth curves
                    fill: true,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#0d6efd',
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            borderDash: [2, 2]
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            }
        });
    }
};
