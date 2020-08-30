import React from "react";
import "./Chart.css";
import useWindowSize from '../hooks/useWindowSize';
import { Media, Label, Container } from 'reactstrap';
import code4RoGrey from '../images/code4RoGrey.svg';
import ElectionPicker from '../services/electionPicker';
import { getVoterTurnoutUrl } from '../services/apiService';
import { useTranslation } from "react-i18next";
import { StackedBar } from "./VoteResults/StackedBar/StackedBar";

const Line = ({ percent }) => (
    <div className="chart-line" style={{ top: `calc(100% - ${percent}%)` }}>
        <div className="chart-line-text">{`${percent}%`}</div>
    </div>
);

const Legend = ({ percent, count, text }) => (
    <div className="legend">
        {percent ? <div className="percentage">{`${percent} %`}</div> : ""}
        <div className={percent ? "count" : "percentage"}>{count}</div>
        <div className="text">{text}</div>
    </div>
);

const StripedBar = ({ color, percent, count, text, orizontal }) => {
    const style = { background: `repeating-linear-gradient(-45deg, ${color} 0 10px, #fff 10px 12px)` }
    if (orizontal) {
        style.width = `${percent}%`;
        style.height = '48px';
    } else {
        style.height = `${percent}%`;
    }
    return (
        <div className="chart-bar" style={style}>
            <Legend percent={percent} count={dotFormat(count)} text={text} />
        </div>
    )
}

const LegendDot = ({ color, text }) => (
    <div style={{ display: 'flex', margin: '5px 0' }}>
        <div style={{
            minWidth: '20px',
            width: '20px',
            height: '20px',
            display: 'inline-block',
            background: `repeating-linear-gradient(-45deg, ${ color } 0 10px, #fff 10px 12px)`,
        } }/>
        <span style={{ paddingLeft: '10px' }}>{text}</span>
    </div>
)

const dotFormat = value => `${value}`.replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1.");

export const ElectionChart = () => {
    const MAX_MOBILE_WIDTH = 575;
    const windowSize = useWindowSize();
    const [data, setData] = React.useState(null);
    
    const { t } = useTranslation();

    const calcPercentage = val => ((val * 100) / data.enlistedVoters).toFixed(2);
    React.useEffect(() => {
        const fetchServerData = async () => {
            try {
                console.log("voter turnout component for " + ElectionPicker.getSelection());
                fetch(getVoterTurnoutUrl(ElectionPicker.getSelection()))
                    .then(result => result.json())
                    .then(result => {
                        setData(result);
                    });
            } catch (e) {
                console.log(e);
            }
        };

        const onIdle = () => {
            clearInterval(timer);
            timer = null;
        }
        const onActive = () => {
            fetchServerData();
            timer = setInterval(() => fetchServerData(), 1000 * 30);
        }
        const onSelectedElectionsChanged = () => {
            fetchServerData();
        }
        window.addEventListener("selectedElectionsChanged", onSelectedElectionsChanged);
        window.addEventListener("onIdle", onIdle);
        window.addEventListener("onActive", onActive);
        fetchServerData();
        let timer = setInterval(() => fetchServerData(), 1000 * 30);
        return () => {
            console.log("cleaned up vote monitoring component");
            clearInterval(timer);
            timer = null;
            window.removeEventListener("onIdle", onIdle);
            window.removeEventListener("onActive", onActive);
        };
    }, []);

    if (!data) {
        return null;
    } 
        return (
            <div className={'full-width-container'}>
                <div className="border-radius">
                    <div className={"vote-monitoring-title-presence"}>
                        <h1>
                            {
                                // TODO: Investigate this, it is collapsing with other components
                                t('voter_turnout')
                            }
                        </h1>
                    </div>
                    <div className="x-container">
                        <div>
                            {windowSize.width > MAX_MOBILE_WIDTH
                                ? <div className="text-center chart-title">{ t('all_voters') }</div>
                                : null
                            }
                            <div className={"info-legend bars bars-spacing"}>
                                <div className={"parent-bar"}>
                                    <p style={{
                                        paddingRight: '20px',
                                        width: 'fit-content',
                                        fontSize: '14px',
                                    }}>
                                        {`100% (${dotFormat(data.enlistedVoters)})`}
                                    </p>
                                </div>
                                <div className="child-bar" style={{ minWidth: '45px', width: `${calcPercentage(data.totalNationalVotes)}%` }}>
                                    <p style={{ fontSize: '14px', color: 'black', width: 'fit-content', }}>
                                        {`${calcPercentage(data.totalNationalVotes)}% (${dotFormat(data.totalNationalVotes)})`}
                                    </p>
                                </div>
                                <div style={{ alignSelf: 'flex-start', marginTop: '35px' }}>
                                    <LegendDot color="#352245" text={ t('eligible_voters') } />
                                    <LegendDot color="#FFCC00" text={ t('voted') } />
                                </div>
                            </div>
                            <div className="chart">
                                {windowSize.width > MAX_MOBILE_WIDTH
                                    ? (
                                        <div>
                                            <Line percent={0} />
                                            <Line percent={25} />
                                            <Line percent={50} />
                                            <Line percent={75} />
                                            <Line percent={100} />
                                        </div>)
                                    : null
                                }
                                <div className="chart-container">
                                    <StripedBar
                                        orizontal={windowSize.width <= MAX_MOBILE_WIDTH}
                                        color="#3C8CD2"
                                        percent={calcPercentage(data.permanentLists)}
                                        count={data.permanentLists}
                                        text={ t('permanent_and_special_list') }
                                    />
                                    <StripedBar
                                        orizontal={windowSize.width <= MAX_MOBILE_WIDTH}
                                        color="#443F46"
                                        percent={calcPercentage(data.additionalLists)}
                                        count={data.additionalLists}
                                        text={ t('additional_list') }
                                    />
                                    <StripedBar
                                        orizontal={windowSize.width <= MAX_MOBILE_WIDTH}
                                        color="#F74B32"
                                        percent={calcPercentage(data.mobileVotes)}
                                        count={data.mobileVotes}
                                        text={ t('mobile_list') }
                                    />
                                </div>
                            </div>
                        </div>
                        <div className={"vote-monitoring-numbers"} style={{ flexFlow: 'column' }}>
                            <div className={"vote-monitoring-area"} style={{ width: '80%' }}>
                                <p style={{ fontSize: '22px' }}>{dotFormat(data.totalDiasporaVotes)}</p>
                                <p style={{ fontSize: '14px', textAlign: 'center' }}> { t('diaspora_voters') } </p>
                            </div>
                        </div>
                        <div>
                            <Container style={{ display: 'flex', alignItems: 'left', justifyContent: 'flex-end', paddingTop: '40px' }}>
                                <Label className="info-label">{ t('developed_by') }</Label>
                                <Media src={code4RoGrey} />
                            </Container>
                        </div>
                    </div>

                    <StackedBar />

                    <p className="becro">{ t('data_source') } <a href="https://prezenta.bec.ro" target="_blank" rel="noopener noreferrer">prezenta.bec.ro</a></p>
                </div>

            </div>
        );
}
