import React from 'react'
import Chart from 'react-google-charts'
import { useTranslation } from 'react-i18next';

const someTestMocks = {
    parties: ['PSD', 'UDMR', 'Altii', 'ALDE', 'PNL'],
    colors: ['red', 'yellow', 'grey', 'aqua', 'blue'],
    votes: [2, 2,3,3,5]
}

export const StackedBar = () => {

    const { t } = useTranslation()

    console.log('t.vote_results', t('vote_results'))
    return (
        <Chart
            // width={'500px'}
            // height={'100px'}
            chartType="BarChart"
            loader={<div>Loading Chart</div>}
            data={[
                ['Partid', ...someTestMocks.parties],
                ['Nr Voturi', ...someTestMocks.votes],
            ]}
         
            options={{
                title:  t('vote_results'),
                chartArea: { width: '50%' },
                isStacked: true,
                colors: someTestMocks.colors,

                hAxis: {
                    
                // title: 'Rezultate Vot MOCK',
                minValue: 0,
                },
                vAxis: {
                },
            }}
            />
    )
}
