import { termsAndConditions } from "./termsAndConditions";
import { privacyPolicy } from "./privacyPolicy";
import { aboutProject } from "./aboutProject";
import { aboutUs } from "./aboutUs";
import { adminPanel } from "./adminPanel";

const baseMessages = {
  "about_the_project": "DESPRE PROIECT",
  "about_us": "DESPRE NOI",
  "round": "TURUL",
  'vote_results': 'Rezultate Vot',
  'voter_turnout': 'PREZENȚĂ LA VOT',
  'all_voters': 'Total alegători',
  'developed_by': 'an app developed by',
  'data_source': 'Date preluate de la',
  'permanent_and_special_list': 'Votanți pe liste permanente și speciale',
  'additional_list': 'Votanți pe liste suplimentare',
  'mobile_list': 'Votanți cu urnă mobilă',
  'diaspora_voters': 'Votanți în Diaspora',
  'donate': 'Donează',
  'useful_links': 'Linkuri Utile',
  'terms_and_conditions': 'Termeni și condiții',
  'privacy_policy': 'Politica de confidențialitate',
  'code_of_conduct': 'Codul de conduită',
  'code_for_romania': 'Code for Romania',
  'contact': 'Contact',
  'subscribe_to_newsletter': 'Abonează-te la newsletter',
  'ong_definition': 'Organizație neguvernamentală independentă, neafiliată politic și apolitică.',
  'copyright': '2019 Code for Romania.',
  'site_description': 'Rezultate Vot transparentizează întreg procesul electoral furnizând în timp real, într-o formă grafică intuitivă, ușor de înțeles, datele oficiale furnizate de către AEP și Birourile Electorale cât și datele preluate prin aplicația Monitorizare Vot dezvoltată de Code for Romania, despre alegerile din România.',
  'counted_votes': 'Voturi numărate',
  'teaser_question': 'Cine va câștiga turul 2 de alegeri prezidențiale?',
  'total': 'Total',
  'national': 'National',
  'diaspora': 'Diaspora',
  'mail': 'Corespondență',
  'display_all_candidates': 'Afiseaza toti candidatii',
  'cancelled_votes': 'din care {{ cancelledVotes }} anulate',
  'vote_monitoring': 'OBSERVAREA INDEPENDENTĂ A ALEGERILOR',
  'vote_monitoring_message': 'Aceste date sunt colectate prin aplicația Monitorizare Vot, dezvoltată de Code for Romania, de la observatorii independenți acreditați în secțiile de votare acoperite.',
  'number_of_visited_polling_stations': 'Secții de votare acoperite',
  'number_of_visited_counties': 'Județe acoperite',
  'number_of_logged_in_observers': 'Observatori logați în aplicație',
  'observer_messages': 'Mesaje trimise de către observatori',
  'observed_issues': 'Probleme sesizate',
  'eligible_voters': 'Cetățeni cu drept de vot',
  'voted': 'Au votat',
  'search': 'Caută',
  'county': 'Județ',
};

export const messages = {
  ...baseMessages,
  ...termsAndConditions,
  ...privacyPolicy,
  ...aboutProject,
  ...aboutUs,
  ...adminPanel,
}
